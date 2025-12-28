using System;
using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using MainDemo.Module.BusinessObjects;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base.General;
using DevExpress.ExpressApp.Security.Strategy;

namespace MainDemo.Module.DatabaseUpdate {
	public class Updater : DevExpress.ExpressApp.Updating.ModuleUpdater {

        static Updater() {
            DevExpress.Persistent.Base.PasswordCryptographer.EnableRfc2898 = true;
            DevExpress.Persistent.Base.PasswordCryptographer.SupportLegacySha512 = false;
            DevExpress.ExpressApp.Security.Strategy.SecuritySystemRole.AutoAssociationPermissions = false;
        }

        public Updater(IObjectSpace objectSpace, Version currentDBVersion) : base(objectSpace, currentDBVersion) { }
        public override void UpdateDatabaseAfterUpdateSchema() {
            base.UpdateDatabaseAfterUpdateSchema();
            Employee administrator = ObjectSpace.FindObject<Employee>(CriteriaOperator.Parse("UserName == 'Admin'"));
            if(administrator == null) {
                administrator = ObjectSpace.CreateObject<Employee>();
                administrator.UserName = "Admin";
                administrator.FirstName = "Admin";
                administrator.LastName = "Admin";
                administrator.IsActive = true;
                administrator.SetPassword("");
                administrator.Roles.Add(GetAdministratorRole());
                administrator.Save();
            }
            //Sam is a manager and he can do everything with his own department
            Employee managerSam = ObjectSpace.FindObject<Employee>(CriteriaOperator.Parse("UserName == 'Sam'"));
            if(managerSam == null) {
                managerSam = ObjectSpace.CreateObject<Employee>();
                managerSam.UserName = "Sam";
                managerSam.FirstName = "Sam";
                managerSam.LastName = "Jackson";
                managerSam.IsActive = true;
                managerSam.SetPassword("");
                managerSam.Roles.Add(GetManagerRole());
                managerSam.Save();
            }
            //John is an ordinary user within the Sam's department.
            Employee userJohn = ObjectSpace.FindObject<Employee>(CriteriaOperator.Parse("UserName == 'John'"));
            if(userJohn == null) {
                userJohn = ObjectSpace.CreateObject<Employee>();
                userJohn.UserName = "John";
                userJohn.FirstName = "John";
                userJohn.LastName = "Doe";
                userJohn.IsActive = true;
                userJohn.SetPassword("");
                userJohn.Roles.Add(GetUserRole());
                userJohn.Save();
            }
            //Mary is a manager of another department.  
            Employee managerMary = ObjectSpace.FindObject<Employee>(CriteriaOperator.Parse("UserName == 'Mary'"));
            if(managerMary == null) {
                managerMary = ObjectSpace.CreateObject<Employee>();
                managerMary.UserName = "Mary";
                managerMary.FirstName = "Mary";
                managerMary.LastName = "Tellinson";
                managerMary.IsActive = true;
                managerMary.SetPassword("");
                managerMary.Roles.Add(GetManagerRole());
                managerMary.Save();
            }
            //Joe is an ordinary user within the Mary's department.
            Employee userJoe = ObjectSpace.FindObject<Employee>(CriteriaOperator.Parse("UserName == 'Joe'"));
            if(userJoe == null) {
                userJoe = ObjectSpace.CreateObject<Employee>();
                userJoe.UserName = "Joe";
                userJoe.FirstName = "Joe";
                userJoe.LastName = "Pitt";
                userJoe.IsActive = true;
                userJoe.SetPassword("");
                userJoe.Roles.Add(GetUserRole());
                userJoe.Save();
            }
            ObjectSpace.CommitChanges();
        }

        //Administrators can do everything within the application.
        private SecuritySystemRole GetAdministratorRole() {
            SecuritySystemRole administratorRole = ObjectSpace.FindObject<SecuritySystemRole>(new BinaryOperator("Name", "Administrators"));
            if(administratorRole == null) {
                administratorRole = ObjectSpace.CreateObject<SecuritySystemRole>();
                administratorRole.Name = "Administrators";
                //Can access everything.
                administratorRole.IsAdministrative = true;
            }
            return administratorRole;
        }
        //Users can access and partially edit data (no create and delete capabilities) from their own department.
        private SecuritySystemRole GetUserRole() {
            SecuritySystemRole userRole = ObjectSpace.FindObject<SecuritySystemRole>(new BinaryOperator("Name", "Users"));
            if(userRole == null) {
                userRole = ObjectSpace.CreateObject<SecuritySystemRole>();
                userRole.Name = "Users";

                SecuritySystemTypePermissionObject userTypePermission = ObjectSpace.CreateObject<SecuritySystemTypePermissionObject>();
                userTypePermission.TargetType = typeof(Employee);
                userRole.TypePermissions.Add(userTypePermission);

                SecuritySystemObjectPermissionsObject canViewEmployeesFromOwnDepartmentObjectPermission = ObjectSpace.CreateObject<SecuritySystemObjectPermissionsObject>();
                canViewEmployeesFromOwnDepartmentObjectPermission.Criteria = "Department.Employees[Oid = CurrentUserId()]";
                //canViewEmployeesFromOwnDepartmentObjectPermission.Criteria = new BinaryOperator(new OperandProperty("Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
                canViewEmployeesFromOwnDepartmentObjectPermission.AllowNavigate = true;
                canViewEmployeesFromOwnDepartmentObjectPermission.AllowRead = true;
                userTypePermission.ObjectPermissions.Add(canViewEmployeesFromOwnDepartmentObjectPermission);
                
                SecuritySystemMemberPermissionsObject canEditOwnUserMemberPermission = ObjectSpace.CreateObject<SecuritySystemMemberPermissionsObject>();
				canEditOwnUserMemberPermission.Members = "ChangePasswordOnFirstLogon; StoredPassword; FirstName; LastName;";
                canEditOwnUserMemberPermission.Criteria = "Oid=CurrentUserId()";
                canEditOwnUserMemberPermission.Criteria = (new OperandProperty("Oid") == new FunctionOperator(CurrentUserIdOperator.OperatorName)).ToString();
                canEditOwnUserMemberPermission.AllowWrite = true;
                userTypePermission.MemberPermissions.Add(canEditOwnUserMemberPermission);

				SecuritySystemMemberPermissionsObject canEditUserAssociationsFromOwnDepartmentMemberPermission = ObjectSpace.CreateObject<SecuritySystemMemberPermissionsObject>();
				canEditUserAssociationsFromOwnDepartmentMemberPermission.Members = "Tasks; Department;";
                canEditUserAssociationsFromOwnDepartmentMemberPermission.Criteria = "Department.Employees[Oid = CurrentUserId()]";
                //canEditUserAssociationsFromOwnDepartmentMemberPermission.Criteria = new BinaryOperator(new OperandProperty("Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
                canEditUserAssociationsFromOwnDepartmentMemberPermission.AllowWrite = true;
                userTypePermission.MemberPermissions.Add(canEditUserAssociationsFromOwnDepartmentMemberPermission);
				
                SecuritySystemTypePermissionObject roleTypePermission = ObjectSpace.CreateObject<SecuritySystemTypePermissionObject>();
                roleTypePermission.TargetType = typeof(SecuritySystemRole);
				roleTypePermission.AllowRead = true;
                userRole.TypePermissions.Add(roleTypePermission);

				SecuritySystemMemberPermissionsObject canEditTaskAssociationsMemberPermission = ObjectSpace.CreateObject<SecuritySystemMemberPermissionsObject>();
				canEditTaskAssociationsMemberPermission.Members = "AssignedTo;";
				canEditTaskAssociationsMemberPermission.Criteria = "AssignedTo.Department.Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canEditTaskAssociationsMemberPermission.Criteria = "AssignedTo.Department.Employees[Oid = CurrentUserId()]";
                //canEditTaskAssociationsMemberPermission.Criteria = new BinaryOperator(new OperandProperty("AssignedTo.Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
				canEditTaskAssociationsMemberPermission.AllowWrite = true;

				SecuritySystemObjectPermissionsObject canyEditTasksFromOwnDepartmentObjectPermission = ObjectSpace.CreateObject<SecuritySystemObjectPermissionsObject>();
				canyEditTasksFromOwnDepartmentObjectPermission.Criteria = "AssignedTo.Department.Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canyEditTasksFromOwnDepartmentObjectPermission.Criteria = "AssignedTo.Department.Employees[Oid = CurrentUserId()]";
                //canyEditTasksFromOwnDepartmentObjectPermission.Criteria = new BinaryOperator(new OperandProperty("AssignedTo.Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
				canyEditTasksFromOwnDepartmentObjectPermission.AllowNavigate = true;
				canyEditTasksFromOwnDepartmentObjectPermission.AllowWrite = true;
				canyEditTasksFromOwnDepartmentObjectPermission.AllowRead = true;

				SecuritySystemObjectPermissionsObject canViewOwnDepartmentObjectPermission = ObjectSpace.CreateObject<SecuritySystemObjectPermissionsObject>();
                canViewOwnDepartmentObjectPermission.Criteria = "Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canViewOwnDepartmentObjectPermission.Criteria = "Employees[Oid=CurrentUserId()]";
                //canViewOwnDepartmentObjectPermission.Criteria = new BinaryOperator(new OperandProperty("Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
                canViewOwnDepartmentObjectPermission.AllowNavigate = true;
                canViewOwnDepartmentObjectPermission.AllowRead = true;
                canViewOwnDepartmentObjectPermission.Save();

				SecuritySystemMemberPermissionsObject canEditAssociationsMemberPermission = ObjectSpace.CreateObject<SecuritySystemMemberPermissionsObject>();
				canEditAssociationsMemberPermission.Members = "Employees;";
				canEditAssociationsMemberPermission.Criteria = "Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canEditAssociationsMemberPermission.Criteria = "Employees[Oid=CurrentUserId()]";
                //canEditAssociationsMemberPermission.Criteria = new BinaryOperator(new OperandProperty("Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
				canEditAssociationsMemberPermission.AllowWrite = true;
            }
            return userRole;
        }
        //Managers can access and fully edit (including create and delete capabilities) data from their own department. However, they cannot access data from other departments.
        private SecuritySystemRole GetManagerRole() {
            SecuritySystemRole managerRole = ObjectSpace.FindObject<SecuritySystemRole>(new BinaryOperator("Name", "Managers"));
            if(managerRole == null) {
                managerRole = ObjectSpace.CreateObject<SecuritySystemRole>();
                managerRole.Name = "Managers";
                managerRole.ChildRoles.Add(GetUserRole());

				SecuritySystemObjectPermissionsObject canEditOwnDepartmentObjectPermission = ObjectSpace.CreateObject<SecuritySystemObjectPermissionsObject>();
                canEditOwnDepartmentObjectPermission.Criteria = "Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canEditOwnDepartmentObjectPermission.Criteria = "Employees[Oid=CurrentUserId()]";
                //canEditOwnDepartmentObjectPermission.Criteria = new BinaryOperator(new OperandProperty("Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
                canEditOwnDepartmentObjectPermission.AllowNavigate = true;
                canEditOwnDepartmentObjectPermission.AllowRead = true;
                canEditOwnDepartmentObjectPermission.AllowWrite = true;
                canEditOwnDepartmentObjectPermission.AllowDelete = true;
                canEditOwnDepartmentObjectPermission.Save();

                SecuritySystemTypePermissionObject employeeTypePermission = ObjectSpace.CreateObject<SecuritySystemTypePermissionObject>();
                employeeTypePermission.TargetType = typeof(Employee);
                employeeTypePermission.AllowNavigate = true;
                employeeTypePermission.AllowCreate = true;
                managerRole.TypePermissions.Add(employeeTypePermission);
                SecuritySystemObjectPermissionsObject canEditEmployeesFromOwnDepartmentObjectPermission = ObjectSpace.CreateObject<SecuritySystemObjectPermissionsObject>();
				canEditEmployeesFromOwnDepartmentObjectPermission.Criteria = "IsNull(Department) || Department.Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canEditEmployeesFromOwnDepartmentObjectPermission.Criteria = "IsNull(Department) || Department.Employees[Oid=CurrentUserId()]";
                //canEditEmployeesFromOwnDepartmentObjectPermission.Criteria = (new NullOperator(new OperandProperty("Department")) | new BinaryOperator(new OperandProperty("Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal)).ToString();
                canEditEmployeesFromOwnDepartmentObjectPermission.AllowWrite = true;
                canEditEmployeesFromOwnDepartmentObjectPermission.AllowDelete = true;
                canEditEmployeesFromOwnDepartmentObjectPermission.AllowNavigate = true;
                canEditEmployeesFromOwnDepartmentObjectPermission.AllowRead = true;
                canEditEmployeesFromOwnDepartmentObjectPermission.Save();
                employeeTypePermission.ObjectPermissions.Add(canEditEmployeesFromOwnDepartmentObjectPermission);


                SecuritySystemObjectPermissionsObject canEditTasksOnlyFromOwnDepartmentObjectPermission = ObjectSpace.CreateObject<SecuritySystemObjectPermissionsObject>();
				canEditTasksOnlyFromOwnDepartmentObjectPermission.Criteria = "IsNull(AssignedTo) || IsNull(AssignedTo.Department) || AssignedTo.Department.Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canEditTasksOnlyFromOwnDepartmentObjectPermission.Criteria = "IsNull(AssignedTo) || IsNull(AssignedTo.Department) || AssignedTo.Department.Employees[Oid=CurrentUserId()]";
                //canEditTasksOnlyFromOwnDepartmentObjectPermission.Criteria = (new NullOperator(new OperandProperty("AssignedTo")) | new NullOperator(new OperandProperty("AssignedTo.Department")) | new BinaryOperator(new OperandProperty("AssignedTo.Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal)).ToString();
                canEditTasksOnlyFromOwnDepartmentObjectPermission.AllowNavigate = true;
                canEditTasksOnlyFromOwnDepartmentObjectPermission.AllowRead = true;
                canEditTasksOnlyFromOwnDepartmentObjectPermission.AllowWrite = true;
                canEditTasksOnlyFromOwnDepartmentObjectPermission.AllowDelete = true;
                canEditTasksOnlyFromOwnDepartmentObjectPermission.Save();
            }
            return managerRole;
        }
    }
}
