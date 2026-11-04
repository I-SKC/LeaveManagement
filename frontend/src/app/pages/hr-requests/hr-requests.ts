import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { finalize } from 'rxjs/operators';

import {
  AuthService,
  EmployeeRecord,
  LeaveRequest,
  LeaveType,
} from '../../services/auth.service';

type PendingAction = {
  requestId: number;
  action: 'approve' | 'reject';
} | null;

@Component({
  selector: 'app-hr-requests',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './hr-requests.html',
  styleUrl: './hr-requests.css',
})
export class HrRequests implements OnInit {
  private readonly api = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  username = this.api.getUsername();
  role = this.api.getRole();
  year = new Date().getFullYear();

  rows: LeaveRequest[] = [];
  types: LeaveType[] = [];
  employees: EmployeeRecord[] = [];

  loading = false;
  error = '';
  message = '';
  search = '';

  pendingAction: PendingAction = null;
  rejectComment = '';

  private processingIds = new Set<number>();

  ngOnInit(): void {
    this.load();
  }

  get filtered(): LeaveRequest[] {
    const query = this.search.trim().toLowerCase();

    if (!query) {
      return this.rows;
    }

    return this.rows.filter((request) => {
      const searchableText = [
        request.leaveRequestId?.toString() ?? '',
        request.employeeId?.toString() ?? '',
        request.leaveTypeId?.toString() ?? '',
        this.employeeLabel(request.employeeId),
        request.reason ?? '',
        request.status ?? '',
        request.startDate ?? '',
        request.endDate ?? '',
      ]
        .join(' ')
        .toLowerCase();

      return searchableText.includes(query);
    });
  }

  get pendingCount(): number {
    return this.filtered.filter((request) => request.status?.toLowerCase() === 'pending').length;
  }

  get approvedCount(): number {
    return this.filtered.filter((request) => request.status?.toLowerCase() === 'approved').length;
  }

  get rejectedCount(): number {
    return this.filtered.filter((request) => request.status?.toLowerCase() === 'rejected').length;
  }

  get pendingRequest(): LeaveRequest | null {
    if (!this.pendingAction) {
      return null;
    }

    return this.rows.find((row) => row.leaveRequestId === this.pendingAction!.requestId) ?? null;
  }

  load(): void {
    if (this.loading) {
      return;
    }

    this.loading = true;
    this.error = '';
    this.message = '';

    this.refreshView();

    this.api.getAllRequests().subscribe({
      next: (requests) => {
        this.rows = requests ?? [];

        forkJoin({
          leaveTypes: this.api.getLeaveTypes().pipe(catchError(() => of([] as LeaveType[]))),
          employees: this.api.getEmployees().pipe(catchError(() => of([] as EmployeeRecord[]))),
        }).subscribe({
          next: (result) => {
            this.types = result.leaveTypes ?? [];
            this.employees = result.employees ?? [];
            this.loading = false;
            this.refreshView();
          },
          error: () => {
            this.types = [];
            this.employees = [];
            this.loading = false;
            this.refreshView();
          },
        });
      },
      error: (error) => {
        console.error('Failed to load all requests:', error);

        this.rows = [];

        if (error.status === 401) {
          this.error = 'Your session has expired. Please sign in again.';
        } else if (error.status === 403) {
          this.error = 'You are not authorized to view all leave requests.';
        } else if (error.status === 0) {
          this.error = 'Cannot connect to the backend. Check that the API is running.';
        } else {
          this.error =
            error.error?.message ?? `Failed to load all requests. HTTP status: ${error.status}.`;
        }

        this.loading = false;
        this.refreshView();
      },
    });
  }

  employeeLabel(employeeId: number): string {
    return (
      this.employees.find((employee) => employee.employeeId === employeeId)?.name ??
      `Employee #${employeeId}`
    );
  }

  employeeEmail(employeeId: number): string {
    return this.employees.find((employee) => employee.employeeId === employeeId)?.email ?? '';
  }

  isPending(request: LeaveRequest): boolean {
    return request.status?.trim().toLowerCase() === 'pending';
  }

  leaveType(leaveTypeId: number): string {
    return (
      this.types.find((type) => type.leaveTypeId === leaveTypeId)?.name ??
      `Leave Type ${leaveTypeId}`
    );
  }

  isProcessing(leaveRequestId: number): boolean {
    return this.processingIds.has(leaveRequestId);
  }

  openApproveConfirmation(request: LeaveRequest): void {
    if (this.processingIds.has(request.leaveRequestId)) {
      return;
    }

    this.rejectComment = '';

    this.pendingAction = {
      requestId: request.leaveRequestId,
      action: 'approve',
    };

    this.error = '';
    this.message = '';
    this.refreshView();
  }

  openRejectDialog(request: LeaveRequest): void {
    if (this.processingIds.has(request.leaveRequestId)) {
      return;
    }

    this.rejectComment = '';

    this.pendingAction = {
      requestId: request.leaveRequestId,
      action: 'reject',
    };

    this.error = '';
    this.message = '';
    this.refreshView();
  }

  clearPendingAction(): void {
    this.pendingAction = null;
    this.rejectComment = '';
    this.refreshView();
  }

  confirmApprove(): void {
    const action = this.pendingAction;

    if (!action || action.action !== 'approve') {
      return;
    }

    const id = action.requestId;

    if (this.processingIds.has(id)) {
      return;
    }

    this.clearPendingAction();

    this.processingIds.add(id);
    this.error = '';
    this.message = '';
    this.refreshView();

    this.api
      .approveLeaveRequest(id)
      .pipe(
        finalize(() => {
          this.processingIds.delete(id);
          this.refreshView();
        }),
      )
      .subscribe({
        next: () => {
          this.rows = this.rows.map((row) =>
            row.leaveRequestId === id ? { ...row, status: 'Approved' } : row,
          );

          this.message = `Leave request #${id} approved successfully.`;
          this.refreshView();
        },
        error: (error) => {
          this.error = error.error?.message ?? `Approval failed (HTTP ${error.status}).`;

          this.refreshView();
        },
      });
  }

  confirmReject(): void {
    const action = this.pendingAction;

    if (!action || action.action !== 'reject') {
      return;
    }

    const id = action.requestId;
    const comment = this.rejectComment.trim();

    if (!comment) {
      this.error = 'Please enter a reason for rejecting this request.';
      this.refreshView();
      return;
    }

    if (this.processingIds.has(id)) {
      return;
    }

    this.clearPendingAction();

    this.processingIds.add(id);
    this.error = '';
    this.message = '';
    this.refreshView();

    this.api
      .rejectLeaveRequest(id, comment)
      .pipe(
        finalize(() => {
          this.processingIds.delete(id);
          this.refreshView();
        }),
      )
      .subscribe({
        next: () => {
          this.rows = this.rows.map((row) =>
            row.leaveRequestId === id ? { ...row, status: 'Rejected' } : row,
          );

          this.message = `Leave request #${id} rejected successfully.`;
          this.refreshView();
        },
        error: (error) => {
          this.error = error.error?.message ?? `Rejection failed (HTTP ${error.status}).`;

          this.refreshView();
        },
      });
  }

  refresh(): void {
    this.load();
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  goToApprovals(): void {
    this.router.navigate(['/approvals']);
  }

  goToApprovalHistory(): void {
    this.router.navigate(['/approval-history']);
  }

  goToAdministration(): void {
    this.router.navigate(['/administration']);
  }

  logout(): void {
    this.api.logout();
    this.router.navigate(['/login']);
  }

  /**
   * The app runs without zone.js, so asynchronous updates
   * need to ask Angular to re-render explicitly.
   */
  private refreshView(): void {
    this.cdr.markForCheck();
  }
}
