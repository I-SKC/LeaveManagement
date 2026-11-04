
import { Injectable, inject } from '@angular/core';
import {
  HttpClient,
  HttpHeaders
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType?: string;
  expiresAtUtc?: string;
  username?: string;
  role?: string;
}

export interface PendingApproval {
  leaveRequestId: number;
  employeeId: number;
  employeeName: string;
  employeeEmail: string;
  departmentId: number;
  departmentName: string;
  leaveTypeId: number;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: string;
  createdAtUtc: string;
}

export interface LeaveRequest {
  leaveRequestId: number;
  employeeId: number;
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: string;
  approvedBy: number | null;
  createdAtUtc: string;
  decisionComment: string | null;
}

export interface LeaveBalance {
  employeeId: number;
  leaveTypeId: number;
  year: number;
  availableDays: number;
  pendingDays: number;
  remainingDays: number;
}

export interface LeaveType {
  leaveTypeId: number;
  name: string;
  annualAllowance: number;
  isActive: boolean;
  requiresManagerApproval: boolean;
}

export interface ManagerHistory {
  leaveRequestId: number;
  employeeId: number;
  employeeName: string;
  employeeEmail: string;
  departmentId: number;
  departmentName: string;
  leaveTypeId: number;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: string;
  createdAtUtc: string;
  decidedAtUtc: string | null;
  approvedBy: number | null;
  approverName: string;
  decisionComment: string | null;
}

export interface EmployeeRecord {
  employeeId: number;
  name: string;
  email: string;
  departmentId: number;
  managerId: number | null;
  joinDate: string;
  isActive: boolean;
}

export interface Department {
  departmentId: number;
  name: string;
  isActive?: boolean;
}

export interface Holiday {
  holidayId?: number;
  name: string;
  holidayDate: string;
  year?: number;
}

export interface CreateLeaveRequest {
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  reason: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}`;
  private readonly authApiUrl = `${this.apiUrl}/Auth`;
  private readonly leaveApiUrl = `${this.apiUrl}/LeaveRequest`;

  // Authentication

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${this.authApiUrl}/login`,
      credentials
    );
  }

  saveToken(token: string): void {
    localStorage.setItem('accessToken', token);
  }

  getToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  saveUserInfo(username: string, role: string): void {
    localStorage.setItem('username', username);
    localStorage.setItem('userRole', role);
  }

  getUsername(): string {
    return localStorage.getItem('username') ?? 'User';
  }

  getRole(): string {
    return localStorage.getItem('userRole') ?? 'Employee';
  }

  // Extract EmployeeId from the JWT payload.
  // The backend remains responsible for validating the token.

  getEmployeeId(): number | null {
    const token = this.getToken();

    if (!token) {
      return null;
    }

    try {
      const payload = token.split('.')[1];

      if (!payload) {
        return null;
      }

      const base64 = payload
        .replace(/-/g, '+')
        .replace(/_/g, '/');

      const paddedBase64 = base64.padEnd(
        Math.ceil(base64.length / 4) * 4,
        '='
      );

      const decodedPayload = decodeURIComponent(
        atob(paddedBase64)
          .split('')
          .map(character =>
            '%' +
            character.charCodeAt(0)
              .toString(16)
              .padStart(2, '0')
          )
          .join('')
      );

      const claims = JSON.parse(decodedPayload);

      const employeeId =
        claims.EmployeeId ??
        claims.employeeId ??
        claims['employee_id'];

      const parsedId = Number(employeeId);

      return Number.isInteger(parsedId) && parsedId > 0
        ? parsedId
        : null;
    } catch {
      return null;
    }
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.getToken();

    return new HttpHeaders({
      Authorization: token ? `Bearer ${token}` : ''
    });
  }

  // Leave requests

  getMyLeaveRequests(
    employeeId: number
  ): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(
      `${this.leaveApiUrl}/employee/${employeeId}`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  createLeaveRequest(
    employeeId: number,
    request: CreateLeaveRequest
  ): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(
      `${this.leaveApiUrl}/employee/${employeeId}`,
      request,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  cancelLeaveRequest(
    requestId: number
  ): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(
      `${this.leaveApiUrl}/${requestId}/cancel`,
      {},
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  // Leave types

  getLeaveTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(
      `${this.apiUrl}/LeaveType`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  createLeaveType(payload: {
    name: string;
    annualAllowance: number;
    requiresManagerApproval: boolean;
  }): Observable<LeaveType> {
    return this.http.post<LeaveType>(
      `${this.apiUrl}/LeaveType`,
      payload,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  updateLeaveType(
    leaveTypeId: number,
    payload: {
      name: string;
      annualAllowance: number;
      requiresManagerApproval: boolean;
    }
  ): Observable<LeaveType> {
    return this.http.put<LeaveType>(
      `${this.apiUrl}/LeaveType/${leaveTypeId}`,
      payload,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  // Retiring a leave type keeps every historical request and balance.
  setLeaveTypeActive(
    leaveTypeId: number,
    isActive: boolean
  ): Observable<LeaveType> {
    const action = isActive ? 'activate' : 'deactivate';

    return this.http.patch<LeaveType>(
      `${this.apiUrl}/LeaveType/${leaveTypeId}/${action}`,
      {},
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  // Leave balances

  getLeaveBalances(
    employeeId: number,
    year = new Date().getFullYear()
  ): Observable<LeaveBalance[]> {
    return this.http.get<LeaveBalance[]>(
      `${this.apiUrl}/LeaveBalance/employee/${employeeId}?year=${year}`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  // Employee profile and records

  getMyProfile(): Observable<EmployeeRecord> {
    return this.http.get<EmployeeRecord>(
      `${this.apiUrl}/Employee/me`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  getEmployees(): Observable<EmployeeRecord[]> {
    return this.http.get<EmployeeRecord[]>(
      `${this.apiUrl}/Employee`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  getEmployee(employeeId: number): Observable<EmployeeRecord> {
    return this.http.get<EmployeeRecord>(
      `${this.apiUrl}/Employee/${employeeId}`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  createEmployee(payload: {
    name: string;
    email: string;
    departmentId: number;
    joinDate: string;
    managerId: number | null;
  }): Observable<EmployeeRecord> {
    return this.http.post<EmployeeRecord>(
      `${this.apiUrl}/Employee`,
      payload,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  updateEmployee(
    employeeId: number,
    payload: {
      name: string;
      email: string;
      departmentId: number;
      managerId: number | null;
    }
  ): Observable<EmployeeRecord> {
    return this.http.put<EmployeeRecord>(
      `${this.apiUrl}/Employee/${employeeId}`,
      payload,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  deactivateEmployee(employeeId: number): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/Employee/${employeeId}/deactivate`,
      {},
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  activateEmployee(employeeId: number): Observable<EmployeeRecord> {
    return this.http.patch<EmployeeRecord>(
      `${this.apiUrl}/Employee/${employeeId}/activate`,
      {},
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  // Departments

  getDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(
      `${this.apiUrl}/Department`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  createDepartment(payload: {
    name: string;
  }): Observable<Department> {
    return this.http.post<Department>(
      `${this.apiUrl}/Department`,
      payload,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  updateDepartment(
    departmentId: number,
    payload: { name: string }
  ): Observable<Department> {
    return this.http.put<Department>(
      `${this.apiUrl}/Department/${departmentId}`,
      payload,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  // Departments referenced by employees are retired, never hard deleted.
  deactivateDepartment(departmentId: number): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/Department/${departmentId}/deactivate`,
      {},
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  // Holidays

  getHolidays(
    year = new Date().getFullYear()
  ): Observable<Holiday[]> {
    return this.http.get<Holiday[]>(
      `${this.apiUrl}/Holiday?year=${year}`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  createHoliday(payload: {
    holidayDate: string;
    name: string;
  }): Observable<Holiday> {
    return this.http.post<Holiday>(
      `${this.apiUrl}/Holiday`,
      payload,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  updateHoliday(
    holidayId: number,
    payload: { holidayDate: string; name: string }
  ): Observable<Holiday> {
    return this.http.put<Holiday>(
      `${this.apiUrl}/Holiday/${holidayId}`,
      payload,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  deleteHoliday(holidayId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/Holiday/${holidayId}`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  // Leave approvals

  getApprovalHistory(
  ): Observable<ManagerHistory[]> {
    return this.http.get<ManagerHistory[]>(
      `${this.apiUrl}/LeaveApproval/history`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  getAllRequests(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(
      `${this.apiUrl}/LeaveRequest/all`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  getPendingApprovals(): Observable<PendingApproval[]> {
    return this.http.get<PendingApproval[]>(
      `${this.apiUrl}/LeaveApproval/pending`,
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  approveLeaveRequest(requestId: number): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/LeaveApproval/${requestId}/approve`,
      {
        comment: ''
      },
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  rejectLeaveRequest(
    requestId: number,
    comment: string
  ): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/LeaveApproval/${requestId}/reject`,
      {
        comment
      },
      {
        headers: this.getAuthHeaders()
      }
    );
  }

  // Logout

  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('username');
    localStorage.removeItem('userRole');
  }
}