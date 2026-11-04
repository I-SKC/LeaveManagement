using System.Text;
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Application.Interfaces.Security;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Application.Services.Authentication;
using LeaveManagement.Application.Services.Employees;
using LeaveManagement.Application.Services.Leave;
using LeaveManagement.Api.Middleware;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Identity;
using LeaveManagement.Domain.Leave;
using LeaveManagement.Infrastructure.Persistence;
using LeaveManagement.Infrastructure.Repositories;
using LeaveManagement.Infrastructure.Security;
using LeaveManagement.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Text.Json.Serialization;

namespace LeaveManagement.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        // Allowed browser origins come from configuration so each environment
        // can restrict CORS without a code change. The Angular dev server is
        // the only default.
        var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"]
                ?? "http://localhost:4200")
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(origin => origin.Trim())
            .Where(origin => origin.Length > 0)
            .ToArray();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        // Controllers and OpenAPI


        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            });

        builder.Services.AddOpenApi();

        // Database
        var connectionString = ResolveConnectionString(builder.Configuration);

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Repositories
        builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        builder.Services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
        builder.Services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        builder.Services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();
        builder.Services.AddScoped<IHolidayRepository, HolidayRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
      

        // Security services
        builder.Services.AddScoped<IPasswordHasher, PasswordHasherService>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Application services
        builder.Services.AddScoped<IEmployeeService, EmployeeService>();
        builder.Services.AddScoped<IDepartmentService, DepartmentService>();
        builder.Services.AddScoped<ILeaveTypeService, LeaveTypeService>();
        builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
        builder.Services.AddScoped<IHolidayService, HolidayService>();
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        builder.Services.AddScoped<ILeaveApprovalService, LeaveApprovalService>();

        // Business clock: date-based rules use the configured business timezone.
        builder.Services.AddSingleton<IBusinessClock>(
            _ => new BusinessClock(builder.Configuration["Business:TimeZone"]));

        // JWT configuration
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is missing.");

        var jwtIssuer = builder.Configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT issuer is missing.");

        var jwtAudience = builder.Configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT audience is missing.");

        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT key must contain at least 32 bytes.");
        }

        // JWT authentication
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,

                    ValidateAudience = true,
                    ValidAudience = jwtAudience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        // Authorization
        // Authorization: Require authentication for all endpoints by default.
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // Swagger with JWT Bearer authentication
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LeaveManagement.Api",
                Version = "v1",
                Description = "Employee Leave Management System API"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT access token."
            });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer", document),
                        new List<string>()
                    }
                });
        });

        var app = builder.Build();

        // Uniform JSON error responses instead of raw exceptions/stack traces.
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseCors("AllowAngular");

        // Development tools
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // Authentication must run before authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers
        app.MapControllers();


        // Seed development reference data, demo employees and login accounts.
        // Runs only in Development and is safe to execute on every startup.
        if (app.Environment.IsDevelopment())
        {
            await SeedDevelopmentDataAsync(app);
        }

        await app.RunAsync();
    }

    // Seeds the Development reference data, demo employees, login accounts and
    // leave balances. Every step checks for existing records first, so restarting
    // the application never creates duplicates. Demo passwords come from user
    // secrets or environment variables and are never written to source control.
    private static async Task SeedDevelopmentDataAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var employeeRepository = scope.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();
        var departmentRepository = scope.ServiceProvider
            .GetRequiredService<IDepartmentRepository>();
        var leaveTypeRepository = scope.ServiceProvider
            .GetRequiredService<ILeaveTypeRepository>();
        var holidayRepository = scope.ServiceProvider
            .GetRequiredService<IHolidayRepository>();
        var leaveBalanceRepository = scope.ServiceProvider
            .GetRequiredService<ILeaveBalanceRepository>();
        var userRepository = scope.ServiceProvider
            .GetRequiredService<IUserRepository>();
        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher>();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var configuration = app.Configuration;

        // Passwords are read from configuration only; never hardcoded here.
        var employeePassword = RequireSeedPassword(
            configuration, "Seed:EmployeePassword");
        var managerPassword = RequireSeedPassword(
            configuration, "Seed:ManagerPassword");
        var hrPassword = RequireSeedPassword(
            configuration, "Seed:HrPassword");

        // Reference data is seeded for 2026 so the demo data set stays stable.
        const short demoYear = 2026;

        // 1. Departments, matched by their unique name.
        var engineering = await EnsureDepartmentAsync("Engineering");
        var humanResources = await EnsureDepartmentAsync("Human Resources");

        // 2. Leave types, matched by their unique name.
        var annualLeave = await EnsureLeaveTypeAsync("Annual Leave", 20.00m);
        var sickLeave = await EnsureLeaveTypeAsync("Sick Leave", 10.00m);
        var casualLeave = await EnsureLeaveTypeAsync("Casual Leave", 7.00m);

        // 3. Illustrative demo holidays only. Replace this list with the real
        // company holiday calendar before using the system in anger.
        (DateOnly Date, string Name)[] demoHolidays =
        {
            (new DateOnly(demoYear, 1, 1), "New Year's Day"),
            (new DateOnly(demoYear, 1, 26), "Republic Day"),
            (new DateOnly(demoYear, 8, 15), "Independence Day"),
            (new DateOnly(demoYear, 10, 2), "Gandhi Jayanti"),
            (new DateOnly(demoYear, 12, 25), "Christmas Day")
        };

        var holidaysByYear = new Dictionary<short, IReadOnlyList<Holiday>>();

        foreach (var (date, name) in demoHolidays)
        {
            var year = checked((short)date.Year);

            if (!holidaysByYear.TryGetValue(year, out var knownHolidays))
            {
                knownHolidays = await holidayRepository.GetByYearAsync(year);
                holidaysByYear[year] = knownHolidays;
            }

            if (knownHolidays.Any(holiday => holiday.HolidayDate == date))
            {
                Console.WriteLine($"Holiday already exists: {date:yyyy-MM-dd}.");
                continue;
            }

            await holidayRepository.AddAsync(new Holiday(date, name));
            await holidayRepository.SaveChangesAsync();

            Console.WriteLine($"Holiday created: {name} ({date:yyyy-MM-dd}).");
        }

        // 4. Employees, created in dependency order so the manager reference
        // always points at an existing employee row.
        var hrAdministrator = await EnsureEmployeeAsync(
            name: "HR Administrator",
            email: "hr@example.com",
            departmentId: humanResources.DepartmentId,
            managerId: null,
            joinDate: new DateOnly(2024, 1, 15));

        var manager = await EnsureEmployeeAsync(
            name: "Department Manager",
            email: "manager@example.com",
            departmentId: engineering.DepartmentId,
            managerId: null,
            joinDate: new DateOnly(2023, 6, 1));

        var demoEmployee = await EnsureEmployeeAsync(
            name: "Demo Employee",
            email: "employee@example.com",
            departmentId: engineering.DepartmentId,
            managerId: manager.EmployeeId,
            joinDate: new DateOnly(2025, 3, 10));

        // 5. Login accounts. The username is the email address, matching
        // AuthenticationService which resolves users by username.
        await EnsureUserAsync(
            hrAdministrator, "hr@example.com", hrPassword, Role.HR);
        await EnsureUserAsync(
            manager, "manager@example.com", managerPassword, Role.Manager);
        await EnsureUserAsync(
            demoEmployee, "employee@example.com", employeePassword, Role.Employee);

        // 6. Leave balances: one row per demo employee and active leave type,
        // initialised to the leave type's annual allowance.
        LeaveType[] seededLeaveTypes = { annualLeave, sickLeave, casualLeave };
        var seededEmployees = new[] { hrAdministrator, manager, demoEmployee };

        foreach (var employee in seededEmployees)
        {
            foreach (var leaveType in seededLeaveTypes)
            {
                await EnsureLeaveBalanceAsync(
                    employee.EmployeeId,
                    leaveType.LeaveTypeId,
                    demoYear,
                    leaveType.AnnualAllowance);
            }
        }

        Console.WriteLine("Development database seeding completed.");

        async Task<Department> EnsureDepartmentAsync(string name)
        {
            var existing = (await departmentRepository.GetAllAsync())
                .FirstOrDefault(department => string.Equals(
                    department.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                Console.WriteLine(
                    $"Department already exists: {existing.Name} (ID {existing.DepartmentId}).");
                return existing;
            }

            var department = new Department(name);

            await departmentRepository.AddAsync(department);
            await departmentRepository.SaveChangesAsync();

            Console.WriteLine(
                $"Department created: {department.Name} (ID {department.DepartmentId}).");

            return department;
        }

        async Task<LeaveType> EnsureLeaveTypeAsync(
            string name,
            decimal annualAllowance)
        {
            var existing = (await leaveTypeRepository.GetAllAsync())
                .FirstOrDefault(leaveType => string.Equals(
                    leaveType.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                Console.WriteLine(
                    $"Leave type already exists: {existing.Name} (ID {existing.LeaveTypeId}).");
                return existing;
            }

            var leaveType = new LeaveType(name, annualAllowance);

            await leaveTypeRepository.AddAsync(leaveType);
            await leaveTypeRepository.SaveChangesAsync();

            Console.WriteLine(
                $"Leave type created: {leaveType.Name} ({leaveType.AnnualAllowance} days, ID {leaveType.LeaveTypeId}).");

            return leaveType;
        }

        async Task<Employee> EnsureEmployeeAsync(
            string name,
            string email,
            int departmentId,
            int? managerId,
            DateOnly joinDate)
        {
            var existing = await employeeRepository.GetByEmailAsync(email);

            if (existing is not null)
            {
                Console.WriteLine(
                    $"Employee already exists: {existing.Email} (ID {existing.EmployeeId}).");
                return existing;
            }

            var employee = new Employee(
                name, email, departmentId, joinDate, managerId);

            await employeeRepository.AddAsync(employee);
            await employeeRepository.SaveChangesAsync();

            Console.WriteLine(
                $"Employee created: {employee.Name} <{employee.Email}> (ID {employee.EmployeeId}).");

            return employee;
        }

        async Task EnsureUserAsync(
            Employee employee,
            string username,
            string password,
            Role role)
        {
            // Guard on both columns that carry a unique index: Username and
            // EmployeeId. One employee may only own a single account.
            var existing = await userRepository.GetByUsernameAsync(username)
                ?? await dbContext.Users.FirstOrDefaultAsync(
                    user => user.EmployeeId == employee.EmployeeId);

            if (existing is not null)
            {
                Console.WriteLine(
                    $"Account already exists: {existing.Username}. No changes made.");
                return;
            }

            var user = new ApplicationUser(
                employee.EmployeeId,
                username,
                passwordHasher.HashPassword(password),
                role);

            await userRepository.AddAsync(user);
            await userRepository.SaveChangesAsync();

            Console.WriteLine(
                $"Development account created: {user.Username} ({user.Role}).");
        }

        async Task EnsureLeaveBalanceAsync(
            int employeeId,
            int leaveTypeId,
            short year,
            decimal availableDays)
        {
            var existing = await leaveBalanceRepository
                .GetByEmployeeAndLeaveTypeAsync(employeeId, leaveTypeId, year);

            if (existing is not null)
            {
                Console.WriteLine(
                    $"Leave balance already exists: employee {employeeId}, leave type {leaveTypeId}, {year}.");
                return;
            }

            await leaveBalanceRepository.AddAsync(
                new LeaveBalance(employeeId, leaveTypeId, year, availableDays));
            await leaveBalanceRepository.SaveChangesAsync();

            Console.WriteLine(
                $"Leave balance created: employee {employeeId}, leave type {leaveTypeId}, {year} ({availableDays} days).");
        }
    }

    // Reads a development seed password from configuration (user secrets or
    // environment variables). The value is never logged or committed.
    private static string RequireSeedPassword(
        IConfiguration configuration,
        string key)
    {
        var password = configuration[key];

        if (!string.IsNullOrWhiteSpace(password))
        {
            return password;
        }

        throw new InvalidOperationException(
            $"Development seeding requires the '{key}' setting. Configure it with " +
            $"'dotnet user-secrets set {key} <password> --project LeaveManagement.Api' " +
            $"or with the '{key.Replace(":", "__")}' environment variable.");
    }

    // Resolves the SQL Server connection string. The topology (host, port,
    // database, user) comes from configuration; the password is supplied
    // separately through user secrets or environment variables so it is never
    // written to a file in source control.
    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set it in " +
                "appsettings.Development.json, with user secrets, or with the " +
                "ConnectionStrings__DefaultConnection environment variable.");
        }

        var connectionStringBuilder = new SqlConnectionStringBuilder(configured);

        // "Database:Password" is intentionally absent from the appsettings files.
        var password = configuration["Database:Password"];

        if (!string.IsNullOrWhiteSpace(password))
        {
            connectionStringBuilder.Password = password;
        }

        if (!connectionStringBuilder.IntegratedSecurity &&
            string.IsNullOrEmpty(connectionStringBuilder.Password))
        {
            throw new InvalidOperationException(
                "No SQL Server password was provided. Set Database:Password with user " +
                "secrets (dotnet user-secrets set \"Database:Password\" \"<password>\") " +
                "or with the Database__Password environment variable.");
        }

        return connectionStringBuilder.ConnectionString;
    }
}