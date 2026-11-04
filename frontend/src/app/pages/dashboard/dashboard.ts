import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import {
  AuthService,
  Department,
  EmployeeRecord,
  Holiday,
  LeaveRequest,
  LeaveType,
  ManagerHistory,
  PendingApproval,
} from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css', './dashboard-hr.css'],
})
export class Dashboard implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  username = this.authService.getUsername();
  role = this.authService.getRole();

  isLoading = false;
  errorMessage = '';

  leaveRequests: LeaveRequest[] = [];
  leaveTypes: LeaveType[] = [];

  private leaveTypeNames = new Map<number, string>();

  leaveStats: {
    label: string;
    value: number;
    icon: string;
    subtitle?: string;
  }[] = [
    { label: 'Total Leave Requests', value: 0, icon: '📋' },
    { label: 'Pending Requests', value: 0, icon: '⏳' },
    { label: 'Approved Requests', value: 0, icon: '✅' },
    { label: 'Rejected Requests', value: 0, icon: '❌' },
  ];

  // Manager dashboard data.
  pendingApprovals: PendingApproval[] = [];
  approvalHistory: ManagerHistory[] = [];
  teamMembers: EmployeeRecord[] = [];
  private departmentNames = new Map<number, string>();

  approvalSummary = {
    pending: 0,
    approved: 0,
    rejected: 0,
    teamSize: 0,
  };

  // HR dashboard data.
  hrEmployees: EmployeeRecord[] = [];
  hrRequests: LeaveRequest[] = [];
  hrHolidays: Holiday[] = [];
  hrDepartments: Department[] = [];

  hrLoading = false;
  hrError = '';

  private hrEmployeeNames = new Map<number, string>();
  private hrLeaveTypeNames = new Map<number, string>();

  ngOnInit(): void {
    this.role = this.authService.getRole();
    this.username = this.authService.getUsername();

    this.loadLeaveTypes();

    if (this.isEmployee) {
      this.loadLeaveRequests();
    } else if (this.isManager) {
      this.loadManagerDashboard();
    } else if (this.isHR) {
      this.loadHRDashboard();
    }
  }

  get normalizedRole(): string {
    return (this.role ?? '').trim().toLowerCase();
  }

  get isEmployee(): boolean {
    return this.normalizedRole === 'employee';
  }

  get isManager(): boolean {
    return this.normalizedRole === 'manager';
  }

  get isHR(): boolean {
    return this.normalizedRole === 'hr';
  }

  get dashboardTitle(): string {
    if (this.isHR) {
      return 'HR Dashboard';
    }

    if (this.isManager) {
      return 'Manager Dashboard';
    }

    return 'Employee Dashboard';
  }

  loadLeaveTypes(): void {
    this.authService.getLeaveTypes().subscribe({
      next: (types) => {
        this.leaveTypes = types ?? [];

        this.leaveTypeNames = new Map(this.leaveTypes.map((type) => [type.leaveTypeId, type.name]));

        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Unable to load leave types:', error);
      },
    });
  }

  loadLeaveRequests(): void {
    if (this.isLoading) {
      return;
    }

    const employeeId = this.authService.getEmployeeId();

    if (employeeId === null) {
      this.errorMessage = 'Employee ID was not found in your login token. Please sign in again.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService
      .getMyLeaveRequests(employeeId)
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: (response) => {
          if (!Array.isArray(response)) {
            console.error('Unexpected leave request API response:', response);

            this.leaveRequests = [];
            this.updateLeaveStats();

            this.errorMessage = 'The API returned an unexpected response format.';

            this.cdr.markForCheck();
            return;
          }

          this.leaveRequests = response;
          this.updateLeaveStats();

          this.cdr.markForCheck();
        },

        error: (error) => {
          console.error('Failed to load leave requests:', error);

          this.leaveRequests = [];
          this.updateLeaveStats();

          if (error.status === 401) {
            this.errorMessage = 'Your session has expired. Please sign in again.';
          } else if (error.status === 403) {
            this.errorMessage = 'You are not authorized to view these leave requests.';
          } else if (error.status === 0) {
            this.errorMessage = 'Cannot connect to the backend. Check that the API is running.';
          } else {
            this.errorMessage =
              error.error?.message ??
              `Failed to load leave requests. HTTP status: ${error.status}.`;
          }

          this.cdr.markForCheck();
        },
      });
  }

  updateLeaveStats(): void {
    this.leaveStats = [
      {
        label: 'Total Leave Requests',
        value: this.leaveRequests.length,
        icon: '📋',
      },
      {
        label: 'Pending Requests',
        value: this.countByStatus('Pending'),
        icon: '⏳',
      },
      {
        label: 'Approved Requests',
        value: this.countByStatus('Approved'),
        icon: '✅',
      },
      {
        label: 'Rejected Requests',
        value: this.countByStatus('Rejected'),
        icon: '❌',
      },
    ];
  }

  countByStatus(status: string): number {
    return this.leaveRequests.filter(
      (request) => request.status?.trim().toLowerCase() === status.toLowerCase(),
    ).length;
  }

  get recentLeaves(): LeaveRequest[] {
    return [...this.leaveRequests]
      .sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())
      .slice(0, 5);
  }

  getLeaveType(leaveTypeId: number): string {
    return this.leaveTypeNames.get(leaveTypeId) ?? `Leave Type ${leaveTypeId}`;
  }

  goToApprovals(): void {
    this.router.navigate(['/approvals']);
  }

  goToHRAdmin(): void {
    this.router.navigate(['/hr-admin']);
  }

  goToProfile(): void {
    this.router.navigate(['/profile']);
  }

  goToApprovalHistory(): void {
    this.router.navigate(['/approval-history']);
  }

  goToApplyLeave(): void {
    this.router.navigate(['/apply-leave']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  // ---- Manager dashboard ----

  private loadManagerDashboard(): void {
    if (this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    this.loadDepartmentNames();

    const employeeId = this.authService.getEmployeeId();

    if (employeeId === null) {
      this.errorMessage = 'Employee ID was not found in your login token. Please sign in again.';
      this.isLoading = false;
      this.cdr.markForCheck();
      return;
    }

    this.authService
      .getApprovalHistory()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe({
        next: (history) => {
          this.approvalHistory = history ?? [];

          this.authService.getPendingApprovals().subscribe({
            next: (pending) => {
              this.pendingApprovals = pending ?? [];

              this.authService.getEmployees().subscribe({
                next: (employees) => {
                  this.teamMembers = (employees ?? []).filter(
                    (member) => member.managerId === employeeId && member.isActive,
                  );

                  this.leaveStats = [
                    {
                      label: 'Pending Approvals',
                      value: this.pendingApprovals.length,
                      icon: '⏳',
                      subtitle: 'Awaiting your decision',
                    },
                    {
                      label: 'Approved',
                      value: this.countHistoryByStatus('Approved'),
                      icon: '✅',
                      subtitle: 'Decided this month',
                    },
                    {
                      label: 'Rejected',
                      value: this.countHistoryByStatus('Rejected'),
                      icon: '❌',
                      subtitle: 'Decided this month',
                    },
                    {
                      label: 'Team Members',
                      value: this.teamMembers.length,
                      icon: '👥',
                      subtitle: 'Direct reports',
                    },
                  ];

                  this.cdr.markForCheck();
                },
                error: () => {
                  this.teamMembers = [];
                  this.leaveStats[3] = {
                    ...this.leaveStats[3],
                    value: 0,
                  };

                  this.cdr.markForCheck();
                },
              });
            },
            error: () => {
              this.pendingApprovals = [];

              this.leaveStats[0] = {
                ...this.leaveStats[0],
                value: 0,
              };

              this.cdr.markForCheck();
            },
          });
        },

        error: (error) => {
          console.error('Failed to load approval history:', error);

          this.approvalHistory = [];

          if (error.status === 401) {
            this.errorMessage = 'Your session has expired. Please sign in again.';
          } else if (error.status === 403) {
            this.errorMessage = 'You are not authorized to view approval history.';
          } else if (error.status === 0) {
            this.errorMessage = 'Cannot connect to the backend. Check that the API is running.';
          } else {
            this.errorMessage =
              error.error?.message ??
              `Failed to load approval history. HTTP status: ${error.status}.`;
          }

          this.cdr.markForCheck();
        },
      });
  }

  private countHistoryByStatus(status: string): number {
    return this.approvalHistory.filter(
      (row) => row.status?.trim().toLowerCase() === status.toLowerCase(),
    ).length;
  }

  get recentDecisions(): ManagerHistory[] {
    return [...this.approvalHistory]
      .sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())
      .slice(0, 5);
  }

  getDepartmentName(departmentId: number): string {
    return this.departmentNames.get(departmentId) ?? '—';
  }

  /**
   * Department names are resolved once when the manager dashboard loads.
   * Looking them up lazily from a template getter would issue a request on
   * every change-detection cycle.
   */
  private loadDepartmentNames(): void {
    if (this.departmentNames.size > 0) {
      return;
    }

    this.authService.getDepartments().subscribe({
      next: (departments) => {
        this.departmentNames = new Map(
          (departments ?? []).map((department) => [department.departmentId, department.name]),
        );

        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Unable to load departments:', error);
      },
    });
  }

  formatDate(value: string): string {
    if (!value) {
      return '—';
    }

    try {
      return new Date(value).toLocaleDateString('en-GB', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
      });
    } catch {
      return value;
    }
  }

  formatDateTime(value: string | null): string {
    if (!value) {
      return '—';
    }

    try {
      return new Date(value).toLocaleString('en-GB', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return value;
    }
  }

  getManagerStatCards(): {
    label: string;
    value: number;
    icon: string;
    subtitle: string;
  }[] {
    return this.leaveStats.map((stat) => ({
      label: stat.label,
      value: stat.value,
      icon: stat.icon,
      subtitle: stat.subtitle ?? '—',
    }));
  }

  // ---- HR dashboard ----

  loadHRDashboard(): void {
    this.hrLoading = true;
    this.hrError = '';
    this.cdr.markForCheck();

    const year = new Date().getFullYear();

    this.authService.getEmployees().subscribe({
      next: (employees) => {
        this.hrEmployees = employees ?? [];

        this.hrEmployeeNames = new Map(
          this.hrEmployees.map((employee) => [employee.employeeId, employee.name]),
        );

        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('HR employee loading failed:', error);
        this.hrError = 'Unable to load employee information.';
        this.cdr.markForCheck();
      },
    });

    this.authService.getAllRequests().subscribe({
      next: (requests) => {
        this.hrRequests = requests ?? [];
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('HR requests loading failed:', error);
        this.hrError = 'Unable to load leave requests.';
        this.cdr.markForCheck();
      },
    });

    this.authService.getLeaveTypes().subscribe({
      next: (types) => {
        this.hrLeaveTypeNames = new Map((types ?? []).map((type) => [type.leaveTypeId, type.name]));

        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('HR leave types loading failed:', error);
        this.cdr.markForCheck();
      },
    });

    this.authService.getHolidays(year).subscribe({
      next: (holidays) => {
        this.hrHolidays = holidays ?? [];
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('HR holidays loading failed:', error);
        this.cdr.markForCheck();
      },
    });

    this.authService.getDepartments().subscribe({
      next: (departments) => {
        this.hrDepartments = departments ?? [];
        this.hrLoading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('HR departments loading failed:', error);
        this.hrLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  get hrActiveEmployees(): number {
    return this.hrEmployees.filter((employee) => employee.isActive).length;
  }

  get hrPendingRequests(): number {
    return this.hrRequests.filter((request) => request.status?.toLowerCase() === 'pending').length;
  }

  get hrApprovedRequests(): number {
    return this.hrRequests.filter((request) => request.status?.toLowerCase() === 'approved').length;
  }

  get hrRejectedRequests(): number {
    return this.hrRequests.filter((request) => request.status?.toLowerCase() === 'rejected').length;
  }

  get hrRecentRequests(): LeaveRequest[] {
    return [...this.hrRequests]
      .sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())
      .slice(0, 5);
  }

  getHREmployeeName(employeeId: number): string {
    return this.hrEmployeeNames.get(employeeId) ?? `Employee ${employeeId}`;
  }

  getHRLeaveTypeName(leaveTypeId: number): string {
    return this.hrLeaveTypeNames.get(leaveTypeId) ?? `Leave Type ${leaveTypeId}`;
  }

  get hrUpcomingHolidays(): Holiday[] {
    const now = new Date();
    const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;

    return this.hrHolidays
      .filter((holiday) => !!holiday.holidayDate && holiday.holidayDate.slice(0, 10) >= today)
      .sort((a, b) => a.holidayDate.slice(0, 10).localeCompare(b.holidayDate.slice(0, 10)))
      .slice(0, 5);
  }

  get hrHolidayYear(): number {
    return this.hrHolidays[0]?.year ?? new Date().getFullYear();
  }

  departmentEmployeeCount(departmentId: number): number {
    return this.hrEmployees.filter(
      (employee) => employee.departmentId === departmentId && employee.isActive,
    ).length;
  }

  goToHRRequests(): void {
    this.router.navigate(['/hr-requests']);
  }

  goToHRAdministration(): void {
    this.router.navigate(['/administration']);
  }
}
