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
            // https://supportcenter.devexpress.com/ticket/details/T1312589
// DevExpress.Persistent.Base.PasswordCryptographer.EnableRfc2898 = true;
            // https://supportcenter.devexpress.com/ticket/details/T1312589
// DevExpress.Persistent.Base.PasswordCryptographer.SupportLegacySha512 = false;
            // https://supportcenter.devexpress.com/ticket/details/T1312589
// DevExpress.ExpressApp.Security.Strategy.SecuritySystemRole.AutoAssociationPermissions = false;
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
        private DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole GetAdministratorRole() {
            DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole administratorRole = ObjectSpace.FindObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole>(new BinaryOperator("Name", "Administrators"));
            if(administratorRole == null) {
                administratorRole = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole>();
                administratorRole.Name = "Administrators";
                //Can access everything.
                administratorRole.IsAdministrative = true;
            }
            return administratorRole;
        }
        //Users can access and partially edit data (no create and delete capabilities) from their own department.
        private DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole GetUserRole() {
            DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole userRole = ObjectSpace.FindObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole>(new BinaryOperator("Name", "Users"));
            if(userRole == null) {
                userRole = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole>();
                userRole.Name = "Users";

                DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyTypePermissionObject userTypePermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyTypePermissionObject>();
                userTypePermission.TargetType = typeof(Employee);
                userRole.TypePermissions.Add(userTypePermission);

                DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject canViewEmployeesFromOwnDepartmentObjectPermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject>();
                canViewEmployeesFromOwnDepartmentObjectPermission.Criteria = "Department.Employees[Oid = CurrentUserId()]";
                //canViewEmployeesFromOwnDepartmentObjectPermission.Criteria = new BinaryOperator(new OperandProperty("Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
                canViewEmployeesFromOwnDepartmentObjectPermission.NavigateState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canViewEmployeesFromOwnDepartmentObjectPermission.ReadState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                userTypePermission.ObjectPermissions.Add(canViewEmployeesFromOwnDepartmentObjectPermission);
                
                DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyMemberPermissionsObject canEditOwnUserMemberPermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyMemberPermissionsObject>();
				canEditOwnUserMemberPermission.Members = "ChangePasswordOnFirstLogon; StoredPassword; FirstName; LastName;";
                canEditOwnUserMemberPermission.Criteria = "Oid=CurrentUserId()";
                canEditOwnUserMemberPermission.Criteria = (new OperandProperty("Oid") == new FunctionOperator(CurrentUserIdOperator.OperatorName)).ToString();
                canEditOwnUserMemberPermission.WriteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                userTypePermission.MemberPermissions.Add(canEditOwnUserMemberPermission);

				DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyMemberPermissionsObject canEditUserAssociationsFromOwnDepartmentMemberPermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyMemberPermissionsObject>();
				canEditUserAssociationsFromOwnDepartmentMemberPermission.Members = "Tasks; Department;";
                canEditUserAssociationsFromOwnDepartmentMemberPermission.Criteria = "Department.Employees[Oid = CurrentUserId()]";
                //canEditUserAssociationsFromOwnDepartmentMemberPermission.Criteria = new BinaryOperator(new OperandProperty("Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
                canEditUserAssociationsFromOwnDepartmentMemberPermission.WriteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                userTypePermission.MemberPermissions.Add(canEditUserAssociationsFromOwnDepartmentMemberPermission);
				
                DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyTypePermissionObject roleTypePermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyTypePermissionObject>();
                roleTypePermission.TargetType = typeof(DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole);
				roleTypePermission.ReadState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                userRole.TypePermissions.Add(roleTypePermission);

				DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyMemberPermissionsObject canEditTaskAssociationsMemberPermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyMemberPermissionsObject>();
				canEditTaskAssociationsMemberPermission.Members = "AssignedTo;";
				canEditTaskAssociationsMemberPermission.Criteria = "AssignedTo.Department.Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canEditTaskAssociationsMemberPermission.Criteria = "AssignedTo.Department.Employees[Oid = CurrentUserId()]";
                //canEditTaskAssociationsMemberPermission.Criteria = new BinaryOperator(new OperandProperty("AssignedTo.Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
				canEditTaskAssociationsMemberPermission.WriteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;

				DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject canyEditTasksFromOwnDepartmentObjectPermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject>();
				canyEditTasksFromOwnDepartmentObjectPermission.Criteria = "AssignedTo.Department.Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canyEditTasksFromOwnDepartmentObjectPermission.Criteria = "AssignedTo.Department.Employees[Oid = CurrentUserId()]";
                //canyEditTasksFromOwnDepartmentObjectPermission.Criteria = new BinaryOperator(new OperandProperty("AssignedTo.Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
				canyEditTasksFromOwnDepartmentObjectPermission.NavigateState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
				canyEditTasksFromOwnDepartmentObjectPermission.WriteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
				canyEditTasksFromOwnDepartmentObjectPermission.ReadState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;

				DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject canViewOwnDepartmentObjectPermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject>();
                canViewOwnDepartmentObjectPermission.Criteria = "Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canViewOwnDepartmentObjectPermission.Criteria = "Employees[Oid=CurrentUserId()]";
                //canViewOwnDepartmentObjectPermission.Criteria = new BinaryOperator(new OperandProperty("Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
                canViewOwnDepartmentObjectPermission.NavigateState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canViewOwnDepartmentObjectPermission.ReadState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canViewOwnDepartmentObjectPermission.Save();

				DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyMemberPermissionsObject canEditAssociationsMemberPermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyMemberPermissionsObject>();
				canEditAssociationsMemberPermission.Members = "Employees;";
				canEditAssociationsMemberPermission.Criteria = "Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canEditAssociationsMemberPermission.Criteria = "Employees[Oid=CurrentUserId()]";
                //canEditAssociationsMemberPermission.Criteria = new BinaryOperator(new OperandProperty("Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
				canEditAssociationsMemberPermission.WriteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
            }
            return userRole;
        }
        //Managers can access and fully edit (including create and delete capabilities) data from their own department. However, they cannot access data from other departments.
        private DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole GetManagerRole() {
            DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole managerRole = ObjectSpace.FindObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole>(new BinaryOperator("Name", "Managers"));
            if(managerRole == null) {
                managerRole = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole>();
                managerRole.Name = "Managers";
                // https://supportcenter.devexpress.com/ticket/details/T1312589
// managerRole.ChildRoles.Add(GetUserRole());

				DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject canEditOwnDepartmentObjectPermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject>();
                canEditOwnDepartmentObjectPermission.Criteria = "Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canEditOwnDepartmentObjectPermission.Criteria = "Employees[Oid=CurrentUserId()]";
                //canEditOwnDepartmentObjectPermission.Criteria = new BinaryOperator(new OperandProperty("Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal).ToString();
                canEditOwnDepartmentObjectPermission.NavigateState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditOwnDepartmentObjectPermission.ReadState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditOwnDepartmentObjectPermission.WriteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditOwnDepartmentObjectPermission.DeleteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditOwnDepartmentObjectPermission.Save();

                DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyTypePermissionObject employeeTypePermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyTypePermissionObject>();
                employeeTypePermission.TargetType = typeof(Employee);
                employeeTypePermission.NavigateState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                employeeTypePermission.CreateState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                managerRole.TypePermissions.Add(employeeTypePermission);
                DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject canEditEmployeesFromOwnDepartmentObjectPermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject>();
				canEditEmployeesFromOwnDepartmentObjectPermission.Criteria = "IsNull(Department) || Department.Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canEditEmployeesFromOwnDepartmentObjectPermission.Criteria = "IsNull(Department) || Department.Employees[Oid=CurrentUserId()]";
                //canEditEmployeesFromOwnDepartmentObjectPermission.Criteria = (new NullOperator(new OperandProperty("Department")) | new BinaryOperator(new OperandProperty("Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal)).ToString();
                canEditEmployeesFromOwnDepartmentObjectPermission.WriteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditEmployeesFromOwnDepartmentObjectPermission.DeleteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditEmployeesFromOwnDepartmentObjectPermission.NavigateState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditEmployeesFromOwnDepartmentObjectPermission.ReadState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditEmployeesFromOwnDepartmentObjectPermission.Save();
                employeeTypePermission.ObjectPermissions.Add(canEditEmployeesFromOwnDepartmentObjectPermission);


                DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject canEditTasksOnlyFromOwnDepartmentObjectPermission = ObjectSpace.CreateObject<DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject>();
				canEditTasksOnlyFromOwnDepartmentObjectPermission.Criteria = "IsNull(AssignedTo) || IsNull(AssignedTo.Department) || AssignedTo.Department.Oid=[<Employee>][Oid=CurrentUserId()].Single(Department.Oid)";
                canEditTasksOnlyFromOwnDepartmentObjectPermission.Criteria = "IsNull(AssignedTo) || IsNull(AssignedTo.Department) || AssignedTo.Department.Employees[Oid=CurrentUserId()]";
                //canEditTasksOnlyFromOwnDepartmentObjectPermission.Criteria = (new NullOperator(new OperandProperty("AssignedTo")) | new NullOperator(new OperandProperty("AssignedTo.Department")) | new BinaryOperator(new OperandProperty("AssignedTo.Department.Oid"), currentlyLoggedEmployeeDepartmemntOid, BinaryOperatorType.Equal)).ToString();
                canEditTasksOnlyFromOwnDepartmentObjectPermission.NavigateState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditTasksOnlyFromOwnDepartmentObjectPermission.ReadState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditTasksOnlyFromOwnDepartmentObjectPermission.WriteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditTasksOnlyFromOwnDepartmentObjectPermission.DeleteState = DevExpress.Persistent.Base.SecurityPermissionState.Allow;
                canEditTasksOnlyFromOwnDepartmentObjectPermission.Save();
            }
            return managerRole;
        }
    }
}
