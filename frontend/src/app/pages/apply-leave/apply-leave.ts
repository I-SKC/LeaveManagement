import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import {
  AuthService,
  Holiday,
  LeaveBalance,
  LeaveType,
  CreateLeaveRequest
} from '../../services/auth.service';

@Component({
  selector: 'app-apply-leave',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './apply-leave.html',
  styleUrl: './apply-leave.css'
})
export class ApplyLeave implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  username = this.authService.getUsername();

  leaveTypes: LeaveType[] = [];

  balances: LeaveBalance[] = [];

  selectedLeaveTypeId: number | null = null;
  startDate = '';
  endDate = '';
  reason = '';

  isLoadingLeaveTypes = false;
  isSubmitting = false;

  errorMessage = '';
  successMessage = '';

  // Holiday dates (yyyy-MM-dd) used by the working-day preview, mirroring
  // the backend WorkingDayCalculator which skips weekends and holidays.
  private readonly holidayDates = new Set<string>();
  private readonly loadedHolidayYears = new Set<number>();
  private readonly loadingHolidayYears = new Set<number>();

  ngOnInit(): void {
    this.loadLeaveTypes();
    this.loadBalances();
    this.loadHolidaysForYear(new Date().getFullYear());
  }

  loadLeaveTypes(): void {
    this.errorMessage = '';
    this.isLoadingLeaveTypes = true;

    this.authService.getLeaveTypes()
      .pipe(
        finalize(() => {
          this.isLoadingLeaveTypes = false;

          // The app runs zoneless change detection, so the view only repaints
          // when a change is explicitly signalled.
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (types) => {
          // Show only active leave types returned by the API.
          this.leaveTypes = (types ?? []).filter(
            type => type.isActive
          );

          if (this.leaveTypes.length === 0) {
            this.errorMessage =
              'No active leave types are available. Please contact HR.';
          }

          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Unable to load leave types:', error);

          if (error.status === 401 || error.status === 403) {
            this.errorMessage =
              'Your session is not authorized. Please sign in again.';
          } else if (error.status === 0) {
            this.errorMessage =
              'Cannot connect to the backend. Please check that the API is running.';
          } else {
            this.errorMessage =
              error.error?.message ||
              'Unable to load leave types. Please try again.';
          }

          this.cdr.markForCheck();
        }
      });
  }

  // Remaining balance per leave type, used to warn before submitting.
  // The API remains the authority for the actual reservation.
  loadBalances(): void {
    const employeeId = this.authService.getEmployeeId();

    if (!employeeId) {
      return;
    }

    this.authService
      .getLeaveBalances(employeeId, new Date().getFullYear())
      .subscribe({
        next: (balances) => {
          this.balances = balances ?? [];

          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Unable to load leave balances:', error);
        }
      });
  }

  get selectedBalance(): LeaveBalance | null {
    if (!this.selectedLeaveTypeId) {
      return null;
    }

    return (
      this.balances.find(
        balance => balance.leaveTypeId === this.selectedLeaveTypeId
      ) ?? null
    );
  }

  get isOverBalance(): boolean {
    const balance = this.selectedBalance;

    return !!balance && this.totalDays > balance.remainingDays;
  }

  today = new Date().toISOString().split('T')[0];

// Requested duration in working days: weekends and holidays are excluded,
// matching the backend calculation that actually reserves the balance.

get totalDays(): number {
  if (!this.startDate || !this.endDate) {
    return 0;
  }

  const start = new Date(`${this.startDate}T00:00:00`);
  const end = new Date(`${this.endDate}T00:00:00`);

  if (
    Number.isNaN(start.getTime()) ||
    Number.isNaN(end.getTime()) ||
    end < start
  ) {
    return 0;
  }

  let workingDays = 0;

  const cursor = new Date(start);

  while (cursor <= end) {
    const isWeekend =
      cursor.getDay() === 0 || cursor.getDay() === 6;

    if (!isWeekend && !this.isHoliday(cursor)) {
      workingDays++;
    }

    cursor.setDate(cursor.getDate() + 1);
  }

  return workingDays;
}

  isHoliday(date: Date): boolean {
    return this.holidayDates.has(this.toIsoDate(date));
  }

  // Loads the holiday list for every calendar year touched by the selection,
  // so the preview stays correct for the current and next year.
  onDateRangeChanged(): void {
    const startYear = this.yearOf(this.startDate);
    const endYear = this.yearOf(this.endDate);

    if (startYear <= 0) {
      return;
    }

    const firstYear = Math.min(startYear, endYear > 0 ? endYear : startYear);
    const lastYear = Math.max(startYear, endYear > 0 ? endYear : startYear);

    for (let year = firstYear; year <= lastYear; year++) {
      this.loadHolidaysForYear(year);
    }
  }

  private loadHolidaysForYear(year: number): void {
    if (
      year <= 0 ||
      this.loadedHolidayYears.has(year) ||
      this.loadingHolidayYears.has(year)
    ) {
      return;
    }

    this.loadingHolidayYears.add(year);

    this.authService.getHolidays(year)
      .pipe(
        finalize(() => {
          this.loadingHolidayYears.delete(year);
        })
      )
      .subscribe({
        next: (holidays: Holiday[]) => {
          for (const holiday of holidays ?? []) {
            if (holiday.holidayDate) {
              this.holidayDates.add(holiday.holidayDate.slice(0, 10));
            }
          }

          this.loadedHolidayYears.add(year);

          this.cdr.markForCheck();
        },
        error: (error) => {
          // The preview degrades to weekend-only counting; submission is
          // still validated authoritatively by the API.
          console.error(`Unable to load holidays for ${year}:`, error);

          this.cdr.markForCheck();
        }
      });
  }

  private yearOf(value: string): number {
    const year = Number((value ?? '').slice(0, 4));

    return Number.isInteger(year) && year > 0 ? year : 0;
  }

  private toIsoDate(date: Date): string {
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');

    return `${date.getFullYear()}-${month}-${day}`;
  }

  submitLeaveRequest(): void {
    this.errorMessage = '';
    this.successMessage = '';

    const employeeId = this.authService.getEmployeeId();

    if (!employeeId) {
      this.errorMessage =
        'Your Employee ID was not found in the login token. Please sign in again or contact HR.';
      return;
    }

    if (!this.selectedLeaveTypeId) {
      this.errorMessage = 'Please select a leave type.';
      return;
    }

    if (!this.startDate || !this.endDate) {
      this.errorMessage = 'Please select both start and end dates.';
      return;
    }

    if (this.startDate > this.endDate) {
      this.errorMessage =
        'The start date cannot be later than the end date.';
      return;
    }

    if (!this.reason.trim()) {
      this.errorMessage = 'Please enter a reason for your leave.';
      return;
    }

    if (this.isOverBalance) {
      this.errorMessage =
        'The requested working days exceed your remaining leave balance.';
      return;
    }

    const request: CreateLeaveRequest = {
      leaveTypeId: this.selectedLeaveTypeId,
      startDate: this.startDate,
      endDate: this.endDate,
      reason: this.reason.trim()
    };

    this.isSubmitting = true;

    this.authService.createLeaveRequest(employeeId, request)
      .pipe(
        finalize(() => {
          this.isSubmitting = false;

          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: () => {
          this.successMessage =
            'Your leave request has been submitted successfully.';

          this.selectedLeaveTypeId = null;
          this.startDate = '';
          this.endDate = '';
          this.reason = '';

          this.cdr.markForCheck();

          // Navigate to the dashboard after a short delay.
          setTimeout(() => {
            this.router.navigate(['/dashboard']);
          }, 1200);
        },
        error: (error) => {
          console.error('Unable to submit leave request:', error);

          if (error.status === 401) {
            this.errorMessage =
              'Your session has expired. Please sign in again.';
          } else if (error.status === 403) {
            this.errorMessage =
              'You are not authorized to submit this leave request.';
          } else if (error.status === 0) {
            this.errorMessage =
              'Cannot connect to the backend. Please check the API.';
          } else {
            this.errorMessage =
              error.error?.message ||
              'Failed to submit the leave request. Please try again.';
          }

          this.cdr.markForCheck();
        }
      });
  }

  goBackToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
