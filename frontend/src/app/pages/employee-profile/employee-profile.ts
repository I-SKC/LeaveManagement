
import {
  Component,
  ChangeDetectorRef,
  inject,
  OnInit
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';

import {
  AuthService,
  EmployeeRecord
} from '../../services/auth.service';

@Component({
  selector: 'app-employee-profile',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './employee-profile.html',
  styleUrl: './employee-profile.css'
})
export class EmployeeProfile implements OnInit {
  private readonly api = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  username = this.api.getUsername();

  employee: EmployeeRecord | null = null;

  loading = true;
  error = '';

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading = true;
    this.error = '';
    this.employee = null;

    this.cdr.markForCheck();

    this.api.getMyProfile()
      .pipe(
        timeout(15000),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();

          console.log(
            'Profile request finished. Loading:',
            this.loading
          );
        })
      )
      .subscribe({
        next: (employee) => {
          console.log('Employee profile response:', employee);

          if (!employee) {
            this.error =
              'No employee profile information was returned.';
            this.cdr.markForCheck();
            return;
          }

          this.employee = employee;

          console.log(
            'Employee assigned to component:',
            this.employee
          );

          this.cdr.markForCheck();
        },

        error: (error: unknown) => {
          console.error(
            'Unable to load employee profile:',
            error
          );

          const err = error as {
            status?: number;
            error?: { message?: string };
            name?: string;
          };

          if (err.name === 'TimeoutError') {
            this.error =
              'The profile request timed out. Please try again.';
          } else if (err.status === 0) {
            this.error =
              'Cannot connect to the backend. Please check that the API is running.';
          } else if (err.status === 401) {
            this.error =
              'Your session has expired. Please sign in again.';
          } else if (err.status === 403) {
            this.error =
              'You are not authorized to view this profile.';
          } else {
            this.error =
              err.error?.message ||
              `Unable to load your profile${
                err.status ? ` (${err.status})` : ''
              }.`;
          }

          this.cdr.markForCheck();
        }
      });
  }

  get initials(): string {
    const name = this.employee?.name?.trim();

    return name ? name.charAt(0).toUpperCase() : 'E';
  }

  get departmentLabel(): string {
    return this.employee?.departmentId != null
      ? `Department ${this.employee.departmentId}`
      : 'Not assigned';
  }

  get managerLabel(): string {
    return this.employee?.managerId != null
      ? `Manager ${this.employee.managerId}`
      : 'Not assigned';
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  logout(): void {
    this.api.logout();
    this.router.navigate(['/login']);
  }
}
