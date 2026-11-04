
import { Routes } from '@angular/router';

import { Login } from './pages/login/login';
import { Dashboard } from './pages/dashboard/dashboard';
import { ApplyLeave } from './pages/apply-leave/apply-leave';
import { MyLeaves } from './pages/my-leaves/my-leaves';
import { LeaveDetails } from './pages/leave-details/leave-details';
import { LeaveBalancePage } from './pages/leave-balance/leave-balance';
import { EmployeeProfile } from './pages/employee-profile/employee-profile';
import { ManagerApprovals } from './pages/manager-approvals/manager-approvals';
import { ApprovalHistory } from './pages/approval-history/approval-history';
import { HrAdmin } from './pages/hr-admin/hr-admin';
import { HrRequests } from './pages/hr-requests/hr-requests';
import { authGuard, roleGuard } from './guards/role.guard';

export const routes: Routes = [
  { path: 'login', component: Login },

  { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },
  { path: 'apply-leave', component: ApplyLeave, canActivate: [authGuard] },
  { path: 'my-leaves', component: MyLeaves, canActivate: [authGuard] },
  { path: 'my-leaves/:id', component: LeaveDetails, canActivate: [authGuard] },
  {
    path: 'leave-balance',
    component: LeaveBalancePage,
    canActivate: [authGuard]
  },
  { path: 'profile', component: EmployeeProfile, canActivate: [authGuard] },

  {
    path: 'approvals',
    component: ManagerApprovals,
    canActivate: [roleGuard('Manager', 'HR')]
  },
  {
    path: 'approval-history',
    component: ApprovalHistory,
    canActivate: [roleGuard('Manager', 'HR')]
  },

  // HR administration is restricted to the HR role in the UI; the API enforces
  // the same rule for every endpoint.
  {
    path: 'administration',
    component: HrAdmin,
    canActivate: [roleGuard('HR')]
  },
  {
    path: 'hr-requests',
    component: HrRequests,
    canActivate: [roleGuard('HR')]
  },

  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];