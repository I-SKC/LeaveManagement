import {
  ChangeDetectorRef,
  Component,
  inject,
  OnInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';

import {
  AuthService,
  LeaveType,
  ManagerHistory
} from '../../services/auth.service';

@Component({
  selector: 'app-approval-history',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './approval-history.html',
  styleUrl: './approval-history.css'
})
export class ApprovalHistory implements OnInit {
  private readonly api = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  username = this.api.getUsername();
  role = this.api.getRole();
  year = new Date().getFullYear();

  rows: ManagerHistory[] = [];
  types: LeaveType[] = [];

  loading = false;
  error = '';
  search = '';

  ngOnInit(): void {
    this.load();
  }

  get filtered(): ManagerHistory[] {
    const query = this.search.trim().toLowerCase();

    if (!query) {
      return this.rows;
    }

    return this.rows.filter((row) => {
      if (!row) {
        return false;
      }

      const searchableText = [
        row.employeeName ?? '',
        row.employeeEmail ?? '',
        row.departmentName ?? '',
        row.reason ?? '',
        row.leaveRequestId.toString(),
        row.leaveTypeName ?? '',
        row.status ?? '',
        row.approverName ?? '',
        row.decisionComment ?? ''
      ]
        .join(' ')
        .toLowerCase();

      return searchableText.includes(query);
    });
  }

  get approvedCount(): number {
    return this.filtered.filter(
      (row) => row.status?.toLowerCase() === 'approved'
    ).length;
  }

  get rejectedCount(): number {
    return this.filtered.filter(
      (row) => row.status?.toLowerCase() === 'rejected'
    ).length;
  }

  load(): void {
    if (this.loading) {
      return;
    }

    this.loading = true;
    this.error = '';

    forkJoin({
      history: this.api.getApprovalHistory(),
      leaveTypes: this.api.getLeaveTypes()
    })
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        }),
        catchError((error) => {
          this.error =
            error.error?.message ??
            `Unable to load approval history (HTTP ${error.status}).`;

          return of({ history: [] as ManagerHistory[], leaveTypes: [] as LeaveType[] });
        })
      )
      .subscribe((result) => {
        this.types = result.leaveTypes ?? [];
        this.rows = result.history ?? [];

        this.cdr.markForCheck();
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

  logout(): void {
    this.api.logout();
    this.router.navigate(['/login']);
  }

  formatDate(value: string): string {
    if (!value) {
      return '—';
    }

    try {
      return new Date(value).toLocaleDateString('en-GB', {
        day: '2-digit',
        month: 'short',
        year: 'numeric'
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
        minute: '2-digit'
      });
    } catch {
      return value;
    }
  }

  getLeaveType(leaveTypeId: number): string {
    return (
      this.types.find((type) => type.leaveTypeId === leaveTypeId)?.name ??
      `Leave Type ${leaveTypeId}`
    );
  }

  private updateView(): void {
    this.cdr.markForCheck();
  }
}
