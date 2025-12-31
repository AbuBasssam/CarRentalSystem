namespace Domain.AppMetaData;

public static class Permissions
{
    public const string ClaimType = "permission";

    public static class Customer
    {
        public const string Register = "Customer.Register";
        public const string VerifyEmail = "Customer.VerifyEmail";
        public const string BrowseCars = "Customer.Cars.Browse";
        public const string SearchCars = "Customer.Cars.Search";
        public const string Bookings_Create = "Customer.Bookings.Create";
        public const string Bookings_ManageOwn = "Customer.Bookings.ManageOwn";
        public const string Invoices_ViewOwn = "Customer.Invoices.ViewOwn";
        public const string Payments_ViewOwn = "Customer.Payments.ViewOwn";
        public const string Profile_Update = "Customer.Profile.Update";

        public static IEnumerable<string> All => new[]
        {
                Register, VerifyEmail, BrowseCars, SearchCars, Bookings_Create,
                Bookings_ManageOwn, Invoices_ViewOwn, Payments_ViewOwn, Profile_Update
        };
    }

    public static class Employee
    {
        public const string Rentals_Process = "Employee.Rentals.Process";
        public const string Rentals_Return = "Employee.Rentals.Return";
        public const string Vehicles_Inspect = "Employee.Vehicles.Inspect";
        public const string Payments_Process = "Employee.Payments.Process";
        public const string Invoices_Issue = "Employee.Invoices.Issue";

        public static IEnumerable<string> All => new[]
        {
                // include customer permissions
                Customer.SearchCars,Customer.BrowseCars,

                // employee-specific
                Rentals_Process, Rentals_Return, Vehicles_Inspect, Payments_Process, Invoices_Issue
        };
    }

    public static class Admin
    {
        // includes all Employee permissions + admin-specific
        public const string Employees_Manage = "Admin.Employees.Manage";
        public const string Vehicles_Create = "Admin.Vehicles.Create";
        public const string Vehicles_Update = "Admin.Vehicles.Update";
        public const string Vehicles_Delete = "Admin.Vehicles.Delete";
        public const string Prices_Manage = "Admin.Prices.Manage";
        public const string Offers_Manage = "Admin.Offers.Manage";
        public const string Reports_View = "Admin.Reports.View";
        public const string System_Settings = "Admin.System.Settings";
        public const string AuditLogs_View = "Admin.AuditLogs.View";

        public static IEnumerable<string> All => new[]
        {

                Employees_Manage, Vehicles_Create, Vehicles_Update, Vehicles_Delete,
                Prices_Manage, Offers_Manage, Reports_View, System_Settings, AuditLogs_View
        };
    }
}
