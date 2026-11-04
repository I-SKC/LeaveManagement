
import {
  Component,
  inject,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import {
  AuthService,
  LeaveBalance,
  LeaveType
} from '../../services/auth.service';

@Component({
  selector: 'app-leave-balance',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './leave-balance.html',
  styleUrl: './leave-balance.css'
})
export class LeaveBalancePage implements OnInit {
  private readonly api = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  username = this.api.getUsername();
  year = new Date().getFullYear();

  balances: LeaveBalance[] = [];
  types: LeaveType[] = [];

  loading = true;
  error = '';

  ngOnInit(): void {
    this.loadLeaveTypes();
    this.loadLeaveBalances();
  }

  private loadLeaveTypes(): void {
    this.api.getLeaveTypes().subscribe({
      next: (response) => {
        this.types = response ?? [];

        console.log('Leave types received:', this.types);

        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load leave types:', error);
      }
    });
  }

  private loadLeaveBalances(): void {
    this.loading = true;
    this.error = '';

    const employeeId = this.api.getEmployeeId();

    console.log('Employee ID from token:', employeeId);
    console.log('Leave balance year:', this.year);

    if (
      employeeId === null ||
      employeeId === undefined ||
      employeeId <= 0
    ) {
      this.error =
        'Employee ID is missing from your login token. Please sign in again.';

      this.loading = false;
      this.cdr.detectChanges();
      return;
    }

    this.api
      .getLeaveBalances(employeeId, this.year)
      .pipe(
        finalize(() => {
          this.loading = false;

          console.log('Leave balance loading finished.');

          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response) => {
          console.log('Leave balances received:', response);

          this.balances = Array.isArray(response)
            ? response
            : [];

          if (this.balances.length === 0) {
            this.error =
              `No leave balance records were returned for ${this.year}.`;
          }

          this.cdr.detectChanges();
        },

        error: (error) => {
          console.error('Leave balance API error:', error);

          if (error.status === 0) {
            this.error =
              'Cannot connect to the backend. Please check that the API is running.';
          } else if (error.status === 401) {
            this.error =
              'Your session has expired. Please sign in again.';
          } else if (error.status === 403) {
            this.error =
              'You are not authorized to view these leave balances.';
          } else {
            this.error =
              error.error?.message ||
              `Unable to load leave balances (HTTP ${error.status}).`;
          }

          this.cdr.detectChanges();
        }
      });
  }

  leaveType(leaveTypeId: number): string {
    return (
      this.types.find(
        (type) => type.leaveTypeId === leaveTypeId
      )?.name ?? `Leave Type ${leaveTypeId}`
    );
  }

  percent(balance: LeaveBalance): number {
    if (balance.availableDays <= 0) {
      return 0;
    }

    return Math.max(
      0,
      Math.min(
        100,
        (balance.remainingDays / balance.availableDays) * 100
      )
    );
  }
}