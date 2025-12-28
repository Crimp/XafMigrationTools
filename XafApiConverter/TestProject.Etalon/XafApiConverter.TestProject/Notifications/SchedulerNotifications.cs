using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Filtering;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule.Notifications;
using DevExpress.Office.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.General;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace FeatureCenter.Module.Notifications {
    [NavigationItem(false)]
    [ImageName("Notifications.Sheduler_with_notifications")]
    public class SchedulerNotifications : Event {
        public SchedulerNotifications(Session session) : base(session) { }
    }

    // TODO: The 'TaskWithNotifications' class has been marked automatically due to usage of types that have no XAF .NET equivalent.
    //       Please review the class and implement necessary changes to ensure compatibility with XAF .NET.
    // NOTE:
    //   - Type 'DevExpress.Persistent.Base.General.TaskImpl' has no equivalent in XAF .NET
    //     TaskImpl has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'DevExpress.Persistent.Base.General.TaskStatus' has no equivalent in XAF .NET
    //     TaskStatus has no equivalent in XAF .NET (loaded from removed-api.txt)
[NavigationItem(false)]
    [ImageName("Notifications.Task_with_notifications")]
    public class TaskWithNotifications : BaseObject, ISupportNotifications {
        private TaskImpl task = new TaskImpl();
        private DateTime? alarmTime;
        private TimeSpan? remindIn;
        private IList<PostponeTime> postponeTimes;
        private void SetAlarmTime(DateTime? startDate, TimeSpan remindTime) {
            alarmTime = ((startDate - DateTime.MinValue) > remindTime) ? startDate - remindTime : DateTime.MinValue;
        }
        [Persistent(nameof(DateCompleted))]
        private DateTime dateCompleted {
            get { return task.DateCompleted; }
            set {
                DateTime oldValue = task.DateCompleted;
                task.DateCompleted = value;
                OnChanged(nameof(dateCompleted), oldValue, task.DateCompleted);
            }
        }
        private IList<PostponeTime> CreatePostponeTimes() {
            IList<PostponeTime> result = PostponeTime.CreateDefaultPostponeTimesList();
            result.Add(new PostponeTime("None", null, "None"));
            result.Add(new PostponeTime("AtStartTime", TimeSpan.Zero, "At Start Time"));
            PostponeTime.SortPostponeTimesList(result);
            return result;
        }
        protected override void OnLoading() {
            task.IsLoading = true;
            base.OnLoading();
        }
        protected override void OnLoaded() {
            base.OnLoaded();
            task.DateCompleted = dateCompleted;
            task.IsLoading = false;
        }
        public TaskWithNotifications(Session session)
            : base(session) {
        }

        public void MarkCompleted() {
            TaskStatus oldStatus = task.Status;
            task.MarkCompleted();
            OnChanged(nameof(Status), oldStatus, task.Status);
        }

        public string Subject {
            get { return task.Subject; }
            set {
                string oldValue = task.Subject;
                task.Subject = value;
                OnChanged(nameof(Subject), oldValue, task.Subject);
            }
        }
        [Size(SizeAttribute.Unlimited), ObjectValidatorIgnoreIssue(typeof(ObjectValidatorLargeNonDelayedMember))]
        public string Description {
            get { return task.Description; }
            set {
                string oldValue = task.Description;
                task.Description = value;
                OnChanged(nameof(Description), oldValue, task.Description);
            }
        }
        public DateTime DueDate {
            get { return task.DueDate; }
            set {
                DateTime oldValue = task.DueDate;
                task.DueDate = value;
                OnChanged(nameof(DueDate), oldValue, task.DueDate);
            }
        }
        public DateTime StartDate {
            get { return task.StartDate; }
            set {
                DateTime oldValue = task.StartDate;
                task.StartDate = value;
                OnChanged(nameof(StartDate), oldValue, task.StartDate);
                if(!IsLoading && oldValue != value && remindIn != null) {
                    SetAlarmTime(value, remindIn.Value);
                }
            }
        }
        public TaskStatus Status {
            get { return task.Status; }
            set {
                TaskStatus oldValue = task.Status;
                task.Status = value;
                OnChanged(nameof(Status), oldValue, task.Status);
            }
        }
        public Int32 PercentCompleted {
            get { return task.PercentCompleted; }
            set {
                Int32 oldValue = task.PercentCompleted;
                task.PercentCompleted = value;
                OnChanged(nameof(PercentCompleted), oldValue, task.PercentCompleted);
            }
        }
        public DateTime DateCompleted {
            get { return dateCompleted; }
        }
        [ImmediatePostData]
        [NonPersistent]
        [ModelDefault("AllowClear", "False")]
        [DataSourceProperty(nameof(PostponeTimeList))]
        [SearchMemberOptions(SearchMemberMode.Exclude)]
        public PostponeTime ReminderTime {
            get {
                if(RemindIn.HasValue) {
                    return PostponeTimeList.Where(x => (x.RemindIn != null && x.RemindIn.Value == remindIn.Value)).FirstOrDefault();
                } else {
                    return PostponeTimeList.Where(x => x.RemindIn == null).FirstOrDefault();
                }
            }
            set {
                if(!IsLoading) {
                    if(value.RemindIn.HasValue) {
                        RemindIn = value.RemindIn.Value;
                    } else {
                        RemindIn = null;
                    }
                }
            }
        }
        [Browsable(false)]
        public IEnumerable<PostponeTime> PostponeTimeList {
            get {
                if(postponeTimes == null) {
                    postponeTimes = CreatePostponeTimes();
                }
                return postponeTimes;
            }
        }
        [Browsable(false)]
        public TimeSpan? RemindIn {
            get { return remindIn; }
            set {
                SetPropertyValue(nameof(RemindIn), ref remindIn, value);
                if(!IsLoading) {
                    if(value != null) {
                        SetAlarmTime(StartDate, value.Value);
                    } else {
                        alarmTime = null;
                    }
                }
            }
        }
        [Browsable(false)]
        public DateTime? AlarmTime {
            get { return alarmTime; }
            set {
                SetPropertyValue(nameof(AlarmTime), ref alarmTime, value);
                if(value == null) {
                    remindIn = null;
                    IsPostponed = false;
                }
            }
        }
        [Browsable(false)]
        public string NotificationMessage {
            get { return Subject; }
        }
        [Browsable(false)]
        public object UniqueId {
            get { return Oid; }
        }
        [Browsable(false)]
        public bool IsPostponed {
            get;
            set;
        }
    }

    public class TaskWithNotificationsController : ViewController {
        private SimpleAction markCompletedAction;
        private void MarkCompletedAction_Execute(object sender, SimpleActionExecuteEventArgs e) {
            ((TaskWithNotifications)View.CurrentObject).MarkCompleted();
        }
        public TaskWithNotificationsController() {
            TargetObjectType = typeof(TaskWithNotifications);
            markCompletedAction = new SimpleAction(this, "MarkCompleted", PredefinedCategory.Edit);
            markCompletedAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
            markCompletedAction.ImageName = "State_Task_Completed";
            markCompletedAction.Execute += MarkCompletedAction_Execute;
        }
    }
}
