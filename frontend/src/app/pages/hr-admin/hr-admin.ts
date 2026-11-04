import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import {
  AuthService,
  Department,
  EmployeeRecord,
  Holiday,
  LeaveType
} from '../../services/auth.service';

interface EmployeeForm {
  employeeId: number | null;
  name: string;
  email: string;
  departmentId: number | null;
  managerId: number | null;
  joinDate: string;
}

interface LeaveTypeForm {
  name: string;
  annualAllowance: number;
  requiresManagerApproval: boolean;
}

interface HolidayForm {
  holidayId: number | null;
  holidayDate: string;
  name: string;
}

@Component({
  selector: 'app-hr-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './hr-admin.html',
  styleUrls: [
    './hr-admin.css',
    './hr-admin-employees.css',
    './hr-admin-catalog.css'
  ]
})
export class HrAdmin implements OnInit {
  private readonly api = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly pageSize = 8;

  username = this.api.getUsername();

  year = new Date().getFullYear();

  employees: EmployeeRecord[] = [];
  departments: Department[] = [];
  leaveTypes: LeaveType[] = [];
  holidays: Holiday[] = [];

  departmentName = '';
  typeName = '';
  allowance = 12;
  requiresApproval = true;

  employeeSearch = '';
  page = 1;

  employeeForm: EmployeeForm = this.emptyEmployeeForm();
  employeeFormOpen = false;
  editingEmployeeId: number | null = null;
  employeeFormError = '';

  editingDepartmentId: number | null = null;
  departmentDraftName = '';
  departmentFormError = '';

  editingLeaveTypeId: number | null = null;
  leaveTypeDraft: LeaveTypeForm = {
    name: '',
    annualAllowance: 0,
    requiresManagerApproval: true
  };
  leaveTypeFormError = '';

  holidayForm: HolidayForm = this.emptyHolidayForm();
  editingHolidayId: number | null = null;
  holidayFormError = '';

  loadingEmployees = false;
  loadingDepartments = false;
  loadingLeaveTypes = false;
  loadingHolidays = false;

  savingDepartment = false;
  savingLeaveType = false;
  savingEmployee = false;
  savingHoliday = false;
  deactivating = false;
  togglingLeaveType = false;
  deletingHoliday = false;

  message = '';
  error = '';

  employeesError = '';
  departmentsError = '';
  leaveTypesError = '';
  holidaysError = '';

  pendingDeactivation: EmployeeRecord | null = null;
  pendingActivation: EmployeeRecord | null = null;
  pendingDepartmentRemoval: Department | null = null;
  pendingLeaveTypeDeactivation: LeaveType | null = null;
  pendingHolidayDeletion: Holiday | null = null;

  ngOnInit(): void {
    this.load();
  }

  get isRefreshing(): boolean {
    return (
      this.loadingEmployees ||
      this.loadingDepartments ||
      this.loadingLeaveTypes ||
      this.loadingHolidays
    );
  }

  get activeEmployeeCount(): number {
    return this.employees.filter(employee => employee.isActive).length;
  }

  get inactiveEmployeeCount(): number {
    return this.employees.filter(employee => !employee.isActive).length;
  }

  get activeLeaveTypeCount(): number {
    return this.leaveTypes.filter(type => type.isActive).length;
  }

  get activeDepartments(): Department[] {
    return this.departments.filter(department => department.isActive !== false);
  }

  get activeManagerOptions(): EmployeeRecord[] {
    return this.employees.filter(
      employee =>
        employee.isActive &&
        employee.employeeId !== this.editingEmployeeId
    );
  }

  get filteredEmployees(): EmployeeRecord[] {
    const query = this.employeeSearch.trim().toLowerCase();

    if (!query) {
      return this.employees;
    }

    return this.employees.filter(employee =>
      [
        employee.name,
        employee.email,
        employee.employeeId.toString(),
        employee.departmentId?.toString() ?? '',
        employee.managerId?.toString() ?? '',
        this.departmentLabel(employee.departmentId),
        this.managerLabel(employee.managerId),
        employee.isActive ? 'active' : 'inactive'
      ]
        .join(' ')
        .toLowerCase()
        .includes(query)
    );
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredEmployees.length / this.pageSize));
  }

  get currentPage(): number {
    return Math.min(this.page, this.totalPages);
  }

  get pagedEmployees(): EmployeeRecord[] {
    const start = (this.currentPage - 1) * this.pageSize;

    return this.filteredEmployees.slice(start, start + this.pageSize);
  }

  get pageStart(): number {
    return this.filteredEmployees.length === 0
      ? 0
      : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(
      this.currentPage * this.pageSize,
      this.filteredEmployees.length
    );
  }

  onSearchChange(): void {
    this.page = 1;
    this.refreshView();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }

    this.page = page;
    this.refreshView();
  }

  load(): void {
    this.message = '';
    this.error = '';

    this.loadEmployees();
    this.loadDepartments();
    this.loadLeaveTypes();
    this.loadHolidays();
  }

  loadEmployees(): void {
    this.loadingEmployees = true;
    this.employeesError = '';
    this.refreshView();

    this.api.getEmployees()
      .pipe(
        finalize(() => {
          this.loadingEmployees = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: employees => {
          this.employees = [...(employees ?? [])].sort((a, b) =>
            a.name.localeCompare(b.name)
          );

          this.refreshView();
        },

        error: error => {
          console.error('Unable to load employees:', error);

          this.employees = [];
          this.employeesError = this.describeError(
            error,
            'Could not load employees. Verify that your account has HR permissions.'
          );

          this.refreshView();
        }
      });
  }

  loadDepartments(): void {
    this.loadingDepartments = true;
    this.departmentsError = '';
    this.refreshView();

    this.api.getDepartments()
      .pipe(
        finalize(() => {
          this.loadingDepartments = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: departments => {
          this.departments = [...(departments ?? [])].sort((a, b) =>
            a.name.localeCompare(b.name)
          );

          this.refreshView();
        },

        error: error => {
          console.error('Unable to load departments:', error);

          this.departments = [];
          this.departmentsError = this.describeError(
            error,
            'Could not load departments.'
          );

          this.refreshView();
        }
      });
  }

  loadLeaveTypes(): void {
    this.loadingLeaveTypes = true;
    this.leaveTypesError = '';
    this.refreshView();

    this.api.getLeaveTypes()
      .pipe(
        finalize(() => {
          this.loadingLeaveTypes = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: leaveTypes => {
          this.leaveTypes = [...(leaveTypes ?? [])].sort((a, b) =>
            a.name.localeCompare(b.name)
          );

          this.refreshView();
        },

        error: error => {
          console.error('Unable to load leave types:', error);

          this.leaveTypes = [];
          this.leaveTypesError = this.describeError(
            error,
            'Could not load leave types.'
          );

          this.refreshView();
        }
      });
  }

  loadHolidays(): void {
    this.loadingHolidays = true;
    this.holidaysError = '';
    this.refreshView();

    this.api.getHolidays(this.year)
      .pipe(
        finalize(() => {
          this.loadingHolidays = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: holidays => {
          this.holidays = [...(holidays ?? [])].sort((a, b) =>
            a.holidayDate.localeCompare(b.holidayDate)
          );

          this.refreshView();
        },

        error: error => {
          console.error('Unable to load holidays:', error);

          this.holidays = [];
          this.holidaysError = this.describeError(
            error,
            `Could not load holidays for ${this.year}.`
          );

          this.refreshView();
        }
      });
  }

  // ---------------------------------------------------------------- employees

  startEmployeeCreate(): void {
    this.resetFeedback();

    this.editingEmployeeId = null;
    this.employeeFormOpen = true;
    this.employeeFormError = '';
    this.employeeForm = {
      ...this.emptyEmployeeForm(),
      departmentId: this.activeDepartments[0]?.departmentId ?? null
    };

    this.refreshView();
  }

  startEmployeeEdit(employee: EmployeeRecord): void {
    this.resetFeedback();

    this.editingEmployeeId = employee.employeeId;
    this.employeeFormOpen = true;
    this.employeeFormError = '';
    this.employeeForm = {
      employeeId: employee.employeeId,
      name: employee.name,
      email: employee.email,
      departmentId: employee.departmentId,
      managerId: employee.managerId,
      joinDate: employee.joinDate?.substring(0, 10) ?? ''
    };

    this.refreshView();
  }

  cancelEmployeeForm(): void {
    this.editingEmployeeId = null;
    this.employeeFormOpen = false;
    this.employeeFormError = '';
    this.employeeForm = this.emptyEmployeeForm();
    this.refreshView();
  }

  saveEmployee(): void {
    const form = this.employeeForm;

    this.message = '';
    this.error = '';
    this.employeeFormError = '';

    const name = form.name.trim();
    const email = form.email.trim();

    if (!name) {
      this.employeeFormError = 'Employee name is required.';
      this.refreshView();
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      this.employeeFormError = 'Enter a valid email address.';
      this.refreshView();
      return;
    }

    if (form.departmentId == null) {
      this.employeeFormError = 'Select a department for the employee.';
      this.refreshView();
      return;
    }

    if (this.savingEmployee) {
      return;
    }

    this.savingEmployee = true;
    this.refreshView();

    const isEdit = this.editingEmployeeId !== null;

    const request = isEdit
      ? this.api.updateEmployee(this.editingEmployeeId as number, {
          name,
          email,
          departmentId: form.departmentId,
          managerId: form.managerId
        })
      : this.api.createEmployee({
          name,
          email,
          departmentId: form.departmentId,
          joinDate: form.joinDate,
          managerId: form.managerId
        });

    request
      .pipe(
        finalize(() => {
          this.savingEmployee = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: employee => {
          this.employees = isEdit
            ? this.employees
                .map(current =>
                  current.employeeId === employee.employeeId ? employee : current
                )
                .sort((a, b) => a.name.localeCompare(b.name))
            : [...this.employees, employee].sort((a, b) =>
                a.name.localeCompare(b.name)
              );

          this.message = isEdit
            ? `${employee.name} was updated successfully.`
            : `${employee.name} was added to the directory.`;

          this.cancelEmployeeForm();
          this.refreshView();
        },

        error: error => {
          console.error('Unable to save employee:', error);

          this.employeeFormError = this.describeError(
            error,
            `Could not save the employee. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  requestDeactivation(employee: EmployeeRecord): void {
    if (!employee.isActive) {
      this.error = `${employee.name} is already inactive.`;
      this.refreshView();
      return;
    }

    this.resetFeedback();
    this.pendingActivation = null;
    this.pendingDeactivation = employee;
    this.refreshView();
  }

  cancelDeactivation(): void {
    this.pendingDeactivation = null;
    this.refreshView();
  }

  confirmDeactivation(): void {
    const employee = this.pendingDeactivation;

    if (!employee || this.deactivating) {
      return;
    }

    this.deactivating = true;
    this.error = '';
    this.refreshView();

    this.api.deactivateEmployee(employee.employeeId)
      .pipe(
        finalize(() => {
          this.deactivating = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: () => {
          this.employees = this.employees.map(current =>
            current.employeeId === employee.employeeId
              ? { ...current, isActive: false }
              : current
          );

          this.pendingDeactivation = null;
          this.message = `${employee.name} was deactivated successfully.`;

          this.refreshView();
        },

        error: error => {
          console.error('Unable to deactivate employee:', error);

          this.error = this.describeError(
            error,
            `Could not deactivate employee. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  requestActivation(employee: EmployeeRecord): void {
    this.resetFeedback();
    this.pendingDeactivation = null;
    this.pendingActivation = employee;
    this.refreshView();
  }

  cancelActivation(): void {
    this.pendingActivation = null;
    this.refreshView();
  }

  confirmActivation(): void {
    const employee = this.pendingActivation;

    if (!employee || this.deactivating) {
      return;
    }

    this.deactivating = true;
    this.error = '';
    this.refreshView();

    this.api.activateEmployee(employee.employeeId)
      .pipe(
        finalize(() => {
          this.deactivating = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: updated => {
          this.employees = this.employees.map(current =>
            current.employeeId === updated.employeeId ? updated : current
          );

          this.pendingActivation = null;
          this.message = `${updated.name} was reactivated successfully.`;

          this.refreshView();
        },

        error: error => {
          console.error('Unable to reactivate employee:', error);

          this.error = this.describeError(
            error,
            `Could not reactivate employee. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  // -------------------------------------------------------------- departments

  addDepartment(): void {
    const name = this.departmentName.trim();

    this.message = '';
    this.error = '';
    this.departmentFormError = '';

    if (!name) {
      this.departmentFormError = 'Please enter a department name.';
      this.refreshView();
      return;
    }

    if (this.savingDepartment) {
      return;
    }

    this.savingDepartment = true;
    this.refreshView();

    this.api.createDepartment({ name })
      .pipe(
        finalize(() => {
          this.savingDepartment = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: department => {
          this.departments = [...this.departments, department].sort((a, b) =>
            a.name.localeCompare(b.name)
          );

          this.departmentName = '';
          this.message = `Department "${department.name}" was created successfully.`;

          this.refreshView();
        },

        error: error => {
          console.error('Unable to create department:', error);

          this.departmentFormError = this.describeError(
            error,
            `Could not create department. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  startDepartmentEdit(department: Department): void {
    this.resetFeedback();
    this.editingDepartmentId = department.departmentId;
    this.departmentDraftName = department.name;
    this.departmentFormError = '';
    this.refreshView();
  }

  cancelDepartmentEdit(): void {
    this.editingDepartmentId = null;
    this.departmentDraftName = '';
    this.departmentFormError = '';
    this.refreshView();
  }

  saveDepartment(): void {
    const departmentId = this.editingDepartmentId;
    const name = this.departmentDraftName.trim();

    this.message = '';
    this.error = '';
    this.departmentFormError = '';

    if (departmentId == null) {
      return;
    }

    if (!name) {
      this.departmentFormError = 'Department name is required.';
      this.refreshView();
      return;
    }

    if (this.savingDepartment) {
      return;
    }

    this.savingDepartment = true;
    this.refreshView();

    this.api.updateDepartment(departmentId, { name })
      .pipe(
        finalize(() => {
          this.savingDepartment = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: department => {
          this.departments = this.departments
            .map(current =>
              current.departmentId === department.departmentId
                ? department
                : current
            )
            .sort((a, b) => a.name.localeCompare(b.name));

          this.message = `Department renamed to "${department.name}".`;

          this.cancelDepartmentEdit();
          this.refreshView();
        },

        error: error => {
          console.error('Unable to rename department:', error);

          this.departmentFormError = this.describeError(
            error,
            `Could not rename the department. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  requestDepartmentRemoval(department: Department): void {
    this.resetFeedback();

    if (this.departmentEmployeeCount(department.departmentId) > 0) {
      this.error =
        `"${department.name}" still has employees assigned. ` +
        'Move them to another department first.';

      this.refreshView();
      return;
    }

    this.pendingDepartmentRemoval = department;
    this.refreshView();
  }

  cancelDepartmentRemoval(): void {
    this.pendingDepartmentRemoval = null;
    this.refreshView();
  }

  confirmDepartmentRemoval(): void {
    const department = this.pendingDepartmentRemoval;

    if (!department || this.savingDepartment) {
      return;
    }

    this.savingDepartment = true;
    this.error = '';
    this.refreshView();

    this.api.deactivateDepartment(department.departmentId)
      .pipe(
        finalize(() => {
          this.savingDepartment = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: () => {
          this.departments = this.departments.map(current =>
            current.departmentId === department.departmentId
              ? { ...current, isActive: false }
              : current
          );

          this.pendingDepartmentRemoval = null;
          this.message = `"${department.name}" was retired from the department list.`;

          this.refreshView();
        },

        error: error => {
          console.error('Unable to retire department:', error);

          this.pendingDepartmentRemoval = null;
          this.error = this.describeError(
            error,
            `Could not retire the department. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  // -------------------------------------------------------------- leave types

  addLeaveType(): void {
    const name = this.typeName.trim();
    const allowance = Number(this.allowance);

    this.message = '';
    this.error = '';
    this.leaveTypeFormError = '';

    if (!name) {
      this.leaveTypeFormError = 'Please enter a leave type name.';
      this.refreshView();
      return;
    }

    if (!Number.isFinite(allowance) || allowance < 0 || allowance > 365) {
      this.leaveTypeFormError =
        'The annual allowance must be between 0 and 365 days.';
      this.refreshView();
      return;
    }

    if (this.savingLeaveType) {
      return;
    }

    this.savingLeaveType = true;
    this.refreshView();

    this.api.createLeaveType({
      name,
      annualAllowance: allowance,
      requiresManagerApproval: this.requiresApproval
    })
      .pipe(
        finalize(() => {
          this.savingLeaveType = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: leaveType => {
          this.leaveTypes = [...this.leaveTypes, leaveType].sort((a, b) =>
            a.name.localeCompare(b.name)
          );

          this.typeName = '';
          this.allowance = 12;
          this.requiresApproval = true;

          this.message = `Leave type "${leaveType.name}" was created successfully.`;

          this.refreshView();
        },

        error: error => {
          console.error('Unable to create leave type:', error);

          this.leaveTypeFormError = this.describeError(
            error,
            `Could not create leave type. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  startLeaveTypeEdit(type: LeaveType): void {
    this.resetFeedback();
    this.editingLeaveTypeId = type.leaveTypeId;
    this.leaveTypeDraft = {
      name: type.name,
      annualAllowance: type.annualAllowance,
      requiresManagerApproval: type.requiresManagerApproval
    };
    this.leaveTypeFormError = '';
    this.refreshView();
  }

  cancelLeaveTypeEdit(): void {
    this.editingLeaveTypeId = null;
    this.leaveTypeFormError = '';
    this.refreshView();
  }

  saveLeaveType(): void {
    const leaveTypeId = this.editingLeaveTypeId;
    const name = this.leaveTypeDraft.name.trim();
    const allowance = Number(this.leaveTypeDraft.annualAllowance);

    this.message = '';
    this.error = '';
    this.leaveTypeFormError = '';

    if (leaveTypeId == null) {
      return;
    }

    if (!name) {
      this.leaveTypeFormError = 'Leave type name is required.';
      this.refreshView();
      return;
    }

    if (!Number.isFinite(allowance) || allowance < 0 || allowance > 365) {
      this.leaveTypeFormError =
        'The annual allowance must be between 0 and 365 days.';
      this.refreshView();
      return;
    }

    if (this.savingLeaveType) {
      return;
    }

    this.savingLeaveType = true;
    this.refreshView();

    this.api.updateLeaveType(leaveTypeId, {
      name,
      annualAllowance: allowance,
      requiresManagerApproval: this.leaveTypeDraft.requiresManagerApproval
    })
      .pipe(
        finalize(() => {
          this.savingLeaveType = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: leaveType => {
          this.leaveTypes = this.leaveTypes
            .map(current =>
              current.leaveTypeId === leaveType.leaveTypeId ? leaveType : current
            )
            .sort((a, b) => a.name.localeCompare(b.name));

          this.message = `Leave type "${leaveType.name}" was updated.`;

          this.cancelLeaveTypeEdit();
          this.refreshView();
        },

        error: error => {
          console.error('Unable to update leave type:', error);

          this.leaveTypeFormError = this.describeError(
            error,
            `Could not update the leave type. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  toggleLeaveType(type: LeaveType): void {
    if (this.togglingLeaveType) {
      return;
    }

    this.resetFeedback();

    // Deactivation hides the policy from new requests, so it needs to be
    // confirmed; reactivation is safe and happens immediately.
    if (type.isActive) {
      this.pendingLeaveTypeDeactivation = type;
      this.refreshView();
      return;
    }

    this.applyLeaveTypeActive(type, true);
  }

  cancelLeaveTypeDeactivation(): void {
    this.pendingLeaveTypeDeactivation = null;
    this.refreshView();
  }

  confirmLeaveTypeDeactivation(): void {
    const type = this.pendingLeaveTypeDeactivation;

    if (!type) {
      return;
    }

    this.pendingLeaveTypeDeactivation = null;
    this.applyLeaveTypeActive(type, false);
  }

  private applyLeaveTypeActive(type: LeaveType, isActive: boolean): void {
    this.togglingLeaveType = true;
    this.error = '';
    this.refreshView();

    this.api.setLeaveTypeActive(type.leaveTypeId, isActive)
      .pipe(
        finalize(() => {
          this.togglingLeaveType = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: updated => {
          this.leaveTypes = this.leaveTypes.map(current =>
            current.leaveTypeId === updated.leaveTypeId ? updated : current
          );

          this.message = isActive
            ? `Leave type "${updated.name}" is active again.`
            : `Leave type "${updated.name}" was deactivated. Existing balances and history are unchanged.`;

          this.refreshView();
        },

        error: error => {
          console.error('Unable to change leave type status:', error);

          this.error = this.describeError(
            error,
            `Could not change the leave type status. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  // ----------------------------------------------------------------- holidays

  startHolidayCreate(): void {
    this.resetFeedback();
    this.editingHolidayId = null;
    this.holidayFormError = '';
    this.holidayForm = this.emptyHolidayForm();
    this.refreshView();
  }

  startHolidayEdit(holiday: Holiday): void {
    this.resetFeedback();
    this.editingHolidayId = holiday.holidayId ?? null;
    this.holidayFormError = '';
    this.holidayForm = {
      holidayId: holiday.holidayId ?? null,
      holidayDate: holiday.holidayDate?.substring(0, 10) ?? '',
      name: holiday.name
    };

    this.refreshView();
  }

  cancelHolidayForm(): void {
    this.editingHolidayId = null;
    this.holidayFormError = '';
    this.holidayForm = this.emptyHolidayForm();
    this.refreshView();
  }

  saveHoliday(): void {
    const name = this.holidayForm.name.trim();
    const holidayDate = this.holidayForm.holidayDate;

    this.message = '';
    this.error = '';
    this.holidayFormError = '';

    if (!holidayDate) {
      this.holidayFormError = 'Select the holiday date.';
      this.refreshView();
      return;
    }

    if (!name) {
      this.holidayFormError = 'Holiday name is required.';
      this.refreshView();
      return;
    }

    if (this.savingHoliday) {
      return;
    }

    this.savingHoliday = true;
    this.refreshView();

    const holidayId = this.holidayForm.holidayId;

    const request = holidayId != null
      ? this.api.updateHoliday(holidayId, { holidayDate, name })
      : this.api.createHoliday({ holidayDate, name });

    request
      .pipe(
        finalize(() => {
          this.savingHoliday = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: holiday => {
          if (holiday.year && holiday.year !== this.year) {
            this.holidays = this.holidays.filter(
              current => current.holidayId !== holiday.holidayId
            );
          } else {
            this.holidays = [
              ...this.holidays.filter(
                current => current.holidayId !== holiday.holidayId
              ),
              holiday
            ].sort((a, b) => a.holidayDate.localeCompare(b.holidayDate));
          }

          this.message = holidayId != null
            ? `Holiday "${holiday.name}" was updated.`
            : `Holiday "${holiday.name}" was added to the calendar.`;

          this.cancelHolidayForm();
          this.refreshView();
        },

        error: error => {
          console.error('Unable to save holiday:', error);

          this.holidayFormError = this.describeError(
            error,
            `Could not save the holiday. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  requestHolidayDeletion(holiday: Holiday): void {
    this.resetFeedback();
    this.pendingHolidayDeletion = holiday;
    this.refreshView();
  }

  cancelHolidayDeletion(): void {
    this.pendingHolidayDeletion = null;
    this.refreshView();
  }

  confirmHolidayDeletion(): void {
    const holiday = this.pendingHolidayDeletion;

    if (!holiday || holiday.holidayId == null || this.deletingHoliday) {
      return;
    }

    this.deletingHoliday = true;
    this.error = '';
    this.refreshView();

    this.api.deleteHoliday(holiday.holidayId)
      .pipe(
        finalize(() => {
          this.deletingHoliday = false;
          this.refreshView();
        })
      )
      .subscribe({
        next: () => {
          this.holidays = this.holidays.filter(
            current => current.holidayId !== holiday.holidayId
          );

          this.pendingHolidayDeletion = null;
          this.message = `Holiday "${holiday.name}" was removed from the calendar.`;

          this.refreshView();
        },

        error: error => {
          console.error('Unable to delete holiday:', error);

          this.error = this.describeError(
            error,
            `Could not delete the holiday. HTTP status: ${error.status}.`
          );

          this.refreshView();
        }
      });
  }

  // ------------------------------------------------------------------ helpers

  departmentLabel(departmentId: number | null | undefined): string {
    if (departmentId == null) {
      return 'Not assigned';
    }

    const department = this.departments.find(
      item => item.departmentId === departmentId
    );

    return department?.name ?? `Department ${departmentId}`;
  }

  managerLabel(managerId: number | null | undefined): string {
    if (managerId == null) {
      return 'Not assigned';
    }

    const manager = this.employees.find(
      employee => employee.employeeId === managerId
    );

    return manager?.name ?? `Manager ${managerId}`;
  }

  departmentEmployeeCount(departmentId: number): number {
    return this.employees.filter(
      employee => employee.departmentId === departmentId && employee.isActive
    ).length;
  }

  trackByEmployee(_index: number, employee: EmployeeRecord): number {
    return employee.employeeId;
  }

  trackByDepartment(_index: number, department: Department): number {
    return department.departmentId;
  }

  trackByLeaveType(_index: number, type: LeaveType): number {
    return type.leaveTypeId;
  }

  trackByHoliday(_index: number, holiday: Holiday): number {
    return holiday.holidayId ?? 0;
  }

  refresh(): void {
    this.load();
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  logout(): void {
    this.api.logout();
    this.router.navigate(['/login']);
  }

  private emptyEmployeeForm(): EmployeeForm {
    return {
      employeeId: null,
      name: '',
      email: '',
      departmentId: null,
      managerId: null,
      joinDate: new Date().toISOString().substring(0, 10)
    };
  }

  private emptyHolidayForm(): HolidayForm {
    return {
      holidayId: null,
      holidayDate: '',
      name: ''
    };
  }

  private resetFeedback(): void {
    this.message = '';
    this.error = '';
  }

  private describeError(
    error: { status?: number; error?: { message?: string } },
    fallback: string
  ): string {
    if (error.status === 401) {
      return 'Your session has expired. Please sign in again.';
    }

    if (error.status === 403) {
      return 'You are not authorized to perform this action.';
    }

    if (error.status === 0) {
      return 'Cannot connect to the backend. Check that the API is running.';
    }

    return error.error?.message ?? fallback;
  }

  /**
   * The app runs without zone.js, so asynchronous updates have to ask
   * Angular to re-render explicitly.
   */
  private refreshView(): void {
    this.cdr.markForCheck();
  }
}
