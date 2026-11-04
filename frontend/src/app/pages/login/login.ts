import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  email = '';
  password = '';
  errorMessage = '';
  isLoading = false;

  onSubmit(): void {
    this.errorMessage = '';

    if (!this.email.trim() || !this.password) {
      this.errorMessage = 'Please enter your email and password.';
      return;
    }

    this.isLoading = true;

    this.authService.login({
      username: this.email.trim(),
      password: this.password
    })
      .pipe(finalize(() => this.isLoading = false))
      .subscribe({
        next: (response) => {
          if (!response.accessToken) {
            this.errorMessage = 'No access token was returned.';
            return;
          }

          this.authService.saveToken(response.accessToken);
          this.authService.saveUserInfo(
            response.username ?? this.email.trim(),
            response.role ?? 'Employee'
          );

          this.router.navigate(['/dashboard']);
        },
        error: (error) => {
          if (error.status === 401) {
            this.errorMessage = 'Invalid email or password.';
          } else if (error.status === 0) {
            this.errorMessage =
              'Cannot connect to the backend. Check that the API is running and CORS is configured.';
          } else {
            this.errorMessage =
              error.error?.message ||
              `Login failed. Server returned status ${error.status}.`;
          }
        }
      });
  }
}