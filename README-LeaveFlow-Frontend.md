LeaveFlow updated project handoff
================================

Included:
- Existing .NET 10 Clean Architecture solution and source files.
- Existing Angular frontend with current login/dashboard/apply-leave functionality.
- Added My Leaves, leave request details, leave balance, employee profile, Manager/HR approval UI, and HR administration UI.
- Updated Angular routes and dashboard navigation.
- Added typed API methods for existing backend endpoints.
- Apply Leave now loads active leave types from the API instead of using hard-coded IDs.

Run:
1. Start the .NET API using the existing solution. The frontend environment currently points to http://localhost:5027/api.
2. Open a terminal in frontend.
3. Run `npm install`.
4. Run `npm start`.
5. Open http://localhost:4200.

Important integration notes:
- The frontend was not successfully build-verified in this environment because npm installation timed out and this environment's Node version (22.16.0) is below Angular 22's declared minimum (22.22.3 or 24.15.0). Use a supported Node version before running `npm install` / `npm run build`.
- Manager/HR approval page uses existing GET Employee and per-employee LeaveRequest endpoints, then existing LeaveApproval approve/reject endpoints. Manager visibility is based on Employee.ManagerId. If the API returns 403 for any request, verify the logged-in user role, EmployeeId claim, and employee ManagerId assignments.
- HR administration supports existing employee directory, employee deactivation, department creation, and leave type creation endpoints. Holiday listing is supported; holiday creation is not present in the current backend API and is therefore not fabricated.
- Employee creation/update DTOs and the current backend do not expose a user-account provisioning workflow. The HR UI does not invent employee credentials or create login accounts.
- Cancellation endpoint is not present in the current LeaveRequestController, so cancellation is not exposed.
- Leave balance reads the existing LeaveBalance endpoint. Values and policy calculations remain backend-controlled.
- Ensure the real appsettings secrets/connection strings are configured locally. Do not commit real credentials.
