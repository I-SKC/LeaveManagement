import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';

import {
  AuthService,
  LeaveRequest,
  LeaveType,
  PendingApproval
} from '../../services/auth.service';

type PendingAction = 'approve' | 'reject' | null;

interface ApprovalEmployee {
  employeeId: number;
  name: string;
  email: string;
  departmentName: string;
}

interface ApprovalRow {
  request: LeaveRequest;
  employee: ApprovalEmployee;
}

@Component({
  selector: 'app-manager-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './manager-approvals.html',
  styleUrl: './manager-approvals.css'
})
export class ManagerApprovals implements OnInit {
  private readonly api = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  username = this.api.getUsername();
  role = this.api.getRole();
  year = new Date().getFullYear();

  rows: ApprovalRow[] = [];
  types: LeaveType[] = [];

  loading = false;
  error = '';
  message = '';
  search = '';

  pendingAction: { requestId: number; action: PendingAction } | null =
    null;
  rejectComment = '';

  private processingIds = new Set<number>();

  ngOnInit(): void {
    this.load();
  }

  get pendingCount(): number {
    return this.rows.length;
  }

  get filtered(): ApprovalRow[] {
    const query = this.search.trim().toLowerCase();

    if (!query) {
      return this.rows;
    }

    return this.rows.filter(({ request, employee }) => {
      const searchableText = [
        employee?.name ?? '',
        employee?.email ?? '',
        employee?.departmentName ?? '',
        request.reason ?? '',
        request.leaveRequestId.toString(),
        this.leaveType(request.leaveTypeId),
        request.status
      ].join(' ').toLowerCase();

      return searchableText.includes(query);
    });
  }

  get pendingRequest(): ApprovalRow | null {
    if (!this.pendingAction) {
      return null;
    }

    const action = this.pendingAction;

    if (!action) {
      return null;
    }

    return this.rows.find(
      (row) =>
        row.request.leaveRequestId === action.requestId
    ) ?? null;
  }

  load(): void {
    if (this.loading) {
      return;
    }

    this.loading = true;
    this.error = '';
    this.message = '';

    this.refreshView();

    forkJoin({
      pending: this.api.getPendingApprovals(),
      leaveTypes: this.api.getLeaveTypes()
    })
      .pipe(
        finalize(() => {
          this.loading = false;
          this.refreshView();
        }),
        catchError(error => {
          this.error =
            error.error?.message ??
            `Unable to load pending requests (HTTP ${error.status}).`;

          return of({ pending: [] as PendingApproval[], leaveTypes: [] as LeaveType[] });
        })
      )
      .subscribe(result => {
        this.types = result.leaveTypes ?? [];
        this.rows = (result.pending ?? []).map(item => this.toRow(item));

        this.refreshView();
      });
  }

  leaveType(leaveTypeId: number): string {
    return (
      this.types.find(type => type.leaveTypeId === leaveTypeId)?.name ??
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
      action: 'approve'
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
      action: 'reject'
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
    this.clearPendingAction();

    if (!action) {
      return;
    }

    const id = action.requestId;

    if (this.processingIds.has(id)) {
      return;
    }

    this.processingIds.add(id);
    this.error = '';
    this.message = '';
    this.refreshView();

    this.api.approveLeaveRequest(id)
      .pipe(
        finalize(() => {
          this.processingIds.delete(id);
          this.refreshView();
        })
      )
      .subscribe({
        next: () => {
          this.rows = this.rows.filter(
            row => row.request.leaveRequestId !== id
          );

          this.message = `Leave request #${id} approved successfully.`;
          this.refreshView();
        },
        error: error => {
          this.error =
            error.error?.message ??
            `Approval failed (HTTP ${error.status}).`;

          this.refreshView();
        }
      });
  }

  confirmReject(): void {
    const action = this.pendingAction;

    if (!action) {
      return;
    }

    const id = action.requestId;
    const comment = this.rejectComment.trim();

    this.clearPendingAction();

    if (!comment) {
      this.error = 'Please enter a reason for rejecting this request.';
      this.pendingAction = action;
      this.refreshView();
      return;
    }

    if (this.processingIds.has(id)) {
      return;
    }

    this.processingIds.add(id);
    this.error = '';
    this.message = '';
    this.refreshView();

    this.api.rejectLeaveRequest(id, comment)
      .pipe(
        finalize(() => {
          this.processingIds.delete(id);
          this.refreshView();
        })
      )
      .subscribe({
        next: () => {
          this.rows = this.rows.filter(
            row => row.request.leaveRequestId !== id
          );

          this.message = `Leave request #${id} rejected successfully.`;
          this.refreshView();
        },
        error: error => {
          this.error =
            error.error?.message ??
            `Rejection failed (HTTP ${error.status}).`;

          this.refreshView();
        }
      });
  }

  refresh(): void {
    this.load();
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  goToApprovalHistory(): void {
    this.router.navigate(['/approval-history']);
  }

  logout(): void {
    this.api.logout();
    this.router.navigate(['/login']);
  }

  private toRow(item: PendingApproval): ApprovalRow {
    return {
      employee: {
        employeeId: item.employeeId,
        name: item.employeeName,
        email: item.employeeEmail,
        departmentName: item.departmentName
      },
      request: {
        leaveRequestId: item.leaveRequestId,
        employeeId: item.employeeId,
        leaveTypeId: item.leaveTypeId,
        startDate: item.startDate,
        endDate: item.endDate,
        totalDays: item.totalDays,
        reason: item.reason,
        status: item.status,
        approvedBy: null,
        createdAtUtc: item.createdAtUtc,
        decisionComment: null
      }
    };
  }

  /**
   * The app runs without zone.js, so every asynchronous update has to ask
   * Angular to re-render explicitly.
   */
  private refreshView(): void {
    this.cdr.markForCheck();
  }
}
