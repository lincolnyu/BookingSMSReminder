// NOTE Comment/undefine the following define for release
//#define SUPPRESS_SENDING_SMS

using Android;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Telephony;
using Android.Views;
using System.Text;
using static Android.Widget.AdapterView;

namespace BookingSMSReminder
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        class Reminder
        {
            public bool Selected;

            public enum StatusEnum
            {
                Pending,
                Error,
                Sent,
                Dismissed
            }

            public StatusEnum Status;

            public string? Name;
            public string? NameInCalendar;
            public string? PhoneNumber;
            public string? StatusDescription;
            public string Message;

            public Data.Contact? Contact;
            public DateTime? StartTime;

            public override string ToString()
            {
                var nameStr = NameInCalendar != null ? $"{NameInCalendar}->{Name}" : Name;
                return Status == StatusEnum.Pending ? $"[{nameStr}, {PhoneNumber}] {Message}" : StatusDescription;
            }
        }

        // TODO reminder listview adapter
        //https://www.youtube.com/watch?v=aUFdgLSEl0g
        class ReminderAdapter : BaseAdapter
        {
            class ViewHolder : Java.Lang.Object
            {
                public CheckBox CheckBox { get; set; }
            }

            LayoutInflater inflater_;
            List<Reminder> reminders_;

            public override int Count => reminders_.Count;

            public ReminderAdapter(Context context, List<Reminder> reminders)
            {
                reminders_ = reminders;
                inflater_ = LayoutInflater.From(context);
            }

            public override Java.Lang.Object? GetItem(int position)
            {
                return null;
            }

            public override long GetItemId(int position)
            {
                return 0;
            }

            class CheckedChangeListener : Java.Lang.Object, CompoundButton.IOnCheckedChangeListener
            {
                private Reminder reminder_;

                public CheckedChangeListener(Reminder reminder)
                {
                    reminder_ = reminder;
                }

                public void OnCheckedChanged(CompoundButton? buttonView, bool isChecked)
                {
                    reminder_.Selected = isChecked;
                }
            }

            public override View? GetView(int position, View? convertView, ViewGroup? parent)
            {
                ViewHolder? holder;

                if (convertView == null)
                {
                    convertView = inflater_.Inflate(Resource.Layout.activity_custom_row, null);

                    CheckBox cb1 = convertView.FindViewById<CheckBox>(Resource.Id.selected);

                    holder = new ViewHolder();
                    holder.CheckBox = cb1;
                    convertView.Tag = holder;

                    var layout = inflater_.Inflate(Resource.Layout.activity_custom_row, parent, false);
                    layout.LongClickable = true;
                }
                else
                {
                    holder = (ViewHolder)convertView.Tag;
                }

                holder.CheckBox.SetOnCheckedChangeListener(new CheckedChangeListener(reminders_[position]));

                var reminder = reminders_[position];

                TextView tv = convertView.FindViewById<TextView>(Resource.Id.message);
                tv.Text = reminder.ToString();

                CheckBox cb = convertView.FindViewById<CheckBox>(Resource.Id.selected);

                if (reminder.Status == Reminder.StatusEnum.Pending)
                {
                    cb.Enabled = true;
                    cb.Checked = reminder.Selected;
                }
                else
                {
                    cb.Enabled = false;
                    cb.Checked = false;
                }

                return convertView;
            }
        }

        private class ReminderChecker
        {
            private MainActivity context_;

            private bool looperPrepared_ = false;

            public ReminderChecker(MainActivity context)
            {
                context_ = context;

                var newThread = new System.Threading.Thread(new ThreadStart(Run));
                newThread.Start();
            }

            public void Run()
            {
                // To run only once for the thread. Looper.MyLooper nullity check seems to not work
                if (!looperPrepared_)
                {
                    Looper.Prepare();
                    looperPrepared_ = true;
                }

                try
                {
                    context_.CheckDelayReminder();
                }
                finally
                {
                    long delayMs = 0;
                    // TODO assign delay MS

                    var dailyRunTime = Utility.GetDailyNotificationTime().ToTimeSpan();
                    var currentTime = DateTime.Now;
                    if (currentTime.TimeOfDay < dailyRunTime)
                    {
                        delayMs = (long)(dailyRunTime - currentTime.TimeOfDay).TotalMilliseconds;
                    }
                    else
                    {
                        delayMs = (long)(dailyRunTime + TimeSpan.FromDays(1) - currentTime.TimeOfDay).TotalMilliseconds;
                    }

                    context_.handler_.PostDelayed(Run, delayMs);
                }
            }

            public void StopRepeatingTask()
            {
                context_.handler_.RemoveCallbacks(Run);
            }
        }

        class ProcessedRemindersLog
        {
            public string LogFileName { get; }


            private Dictionary<string, List<DateTime>> cache_ = new Dictionary<string, List<DateTime>>();
            private bool cacheInvalid_ = true;

            public ProcessedRemindersLog(string logFileName)
            {
                LogFileName = logFileName;
                cacheInvalid_ = true;
            }

            private void ReCacheIfNeeded()
            {
                lock (this)
                {
                    if (!cacheInvalid_) return;

                    var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                    var logFile = Path.Combine(appDataPath, LogFileName);
                    if (File.Exists(logFile))
                    {
                        using var sr = new StreamReader(logFile);
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            if (string.IsNullOrEmpty(line)) continue;
                            var segs = line.Split('|');
                            if (segs.Length != 2) continue;
                            var number = segs[0];
                            var stt = DateTime.Parse(segs[1]);
                            if (!cache_.TryGetValue(number, out var dates))
                            {
                                dates = new List<DateTime>();
                                cache_.Add(number, dates);
                            }
                            dates.Add(stt);
                        }
                    }

                    cacheInvalid_ = false;
                }
            }

            public static bool IsDatePastForReminding(DateTime dtStart)
            {
                return dtStart.Date - DateTime.Now.Date < TimeSpan.FromDays(1);
            }

            public bool GetIfMessageLogged(Data.Contact contact, DateTime startTime)
            {
                ReCacheIfNeeded();

                if (string.IsNullOrWhiteSpace(contact.MostLikelyNumber) || !cache_.TryGetValue(contact.MostLikelyNumber, out var startDates))
                {
                    return false;
                }
                
                foreach (var stt in startDates)
                {
                    if (stt == startTime)
                    {
                        return true;
                    }
                }
                return false;
            }

            public void CleanUpLogAndAddNewEntry((Data.Contact, DateTime)? newEntry)
            {
                lock (this)
                {
                    var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                    var logFile = Path.Combine(appDataPath, LogFileName);
                    bool newEntryAlreadyExists = false;
                    var lines = new List<string>();
                    if (File.Exists(logFile))
                    {
                        {
                            using var sr = new StreamReader(logFile);
                            while (!sr.EndOfStream)
                            {
                                var line = sr.ReadLine();
                                if (string.IsNullOrEmpty(line)) continue;
                                var segs = line.Split('|');
                                if (segs.Length != 2) continue;
                                var stt = DateTime.Parse(segs[1]);
                                if (!IsDatePastForReminding(stt))
                                {
                                    lines.Add(line);
                                    if (newEntry != null)
                                    {
                                        if (segs[0] == newEntry.Value.Item1.MostLikelyNumber && stt == newEntry.Value.Item2)
                                        {
                                            newEntryAlreadyExists = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        using var sw = new StreamWriter(logFile);
                        foreach (var line in lines)
                        {
                            sw.WriteLine(line);
                        }
                        if (newEntry.HasValue && !newEntryAlreadyExists)
                        {
                            sw.WriteLine($"{newEntry.Value.Item1.MostLikelyNumber}|{newEntry.Value.Item2}");
                        }
                    }

                    cacheInvalid_ = true;
                }
            }
        }


        private ProcessedRemindersLog sentRemindersLog_ = new ProcessedRemindersLog("sent_reminders.log");
        private ProcessedRemindersLog dismissedRemindersLog_ = new ProcessedRemindersLog("dismissed_reminders.log");

        private ListView? listViewReminders_;

        private ReminderAdapter remindersAdapter_;
        private List<Reminder> reminders_ = new List<Reminder>();

        private Handler handler_;
        private ReminderChecker reminderChecker_;

        /// <summary>
        ///  Whether OnCreate() has been called and the class has been initialized.
        /// </summary>
        /// <remarks>
        ///  Some methods may be called prior to OnCreate() and actions in them may only be executed after initialization.
        /// </remarks>
        private bool initializedAndCreated_ = false;

        /// <summary>
        ///  Cache the result of CheckPermission() so CheckPermission() only needs to be called once app-wide.
        /// </summary>
        private bool? checkPermissionResult_ = null;

        /// <summary>
        ///  ValidateSettingOnFirstRun() is run only once and this class may be created multiple times, and that's why this flag is static.
        /// </summary>
        private static bool validateSettingsOnFirstRunHasBeenRun_ = false;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            if (checkPermissionResult_ == null)
            {
                checkPermissionResult_ = CheckPermissions();
            }
            if (checkPermissionResult_ == false)
            {
                return;
            }

            sentRemindersLog_.CleanUpLogAndAddNewEntry(null);
            dismissedRemindersLog_.CleanUpLogAndAddNewEntry(null);

            listViewReminders_ = FindViewById<ListView>(Resource.Id.listview_reminders);
            RegisterForContextMenu(listViewReminders_);

            remindersAdapter_ = new ReminderAdapter(this, reminders_);
            listViewReminders_.Adapter = remindersAdapter_;

            remindersAdapter_.NotifyDataSetChanged();

            var buttonSelectAll = FindViewById<Button>(Resource.Id.button_select_all);
            buttonSelectAll.Click += ButtonSelectAll_Click;

            var buttonSelectNone = FindViewById<Button>(Resource.Id.button_select_none);
            buttonSelectNone.Click += ButtonSelectNone_Click;

            var buttonSendSelected = FindViewById<Button>(Resource.Id.button_send_selected);
            buttonSendSelected.Click += ButtonSendSelected_Click;

            var buttonDismissSelected = FindViewById<Button>(Resource.Id.button_dismiss_selected);
            buttonDismissSelected.Click += ButtonDismissSelected_Click;

            var buttonAddBooking = FindViewById<Button>(Resource.Id.button_add_booking);
            buttonAddBooking.Click += ButtonAddBooking_Click;

            var buttonSettings = FindViewById<Button>(Resource.Id.button_settings);
            buttonSettings.Click += ButtonSettings_Click;

            Data.Instance.ReloadContacts(this);

            handler_ = new Handler();

            StartRepeatingTask();

            if (!validateSettingsOnFirstRunHasBeenRun_)
            {
                // Run this only onece
                ValidateSettingsOnFirstRun();
                validateSettingsOnFirstRunHasBeenRun_ = true;
            }

            initializedAndCreated_ = true;
        }


        /// <summary>
        /// Check permissions and return false if mandatory ones are not granted.
        /// </summary>
        /// <returns>False if mandatory permissions are not granted.</returns>
        private bool CheckPermissions()
        {
            var ungrantedMandatory = new List<string>();
            var ungrantedOptional = new List<string>();
            if (CheckSelfPermission(Manifest.Permission.ReadCalendar) != Android.Content.PM.Permission.Granted)
            {
                ungrantedMandatory.Add("Read Calendar");
            }

            if (CheckSelfPermission(Manifest.Permission.ReadContacts) != Android.Content.PM.Permission.Granted)
            {
                ungrantedMandatory.Add("Read Contacts");
            }

            if (CheckSelfPermission(Manifest.Permission.PostNotifications) != Android.Content.PM.Permission.Granted)
            {
                ungrantedOptional.Add("Post Notifications");
            }

            if (CheckSelfPermission(Manifest.Permission.SendSms) != Android.Content.PM.Permission.Granted)
            {
                ungrantedMandatory.Add("Send SMS");
            }

            if (ungrantedMandatory.Count > 0)
            {
                Utility.ShowAlert(this, "Error: Ungranted Permissions", $"Grant the following permissions and then relaunch the app.\n{string.Join('\n', ungrantedMandatory)}", "OK", () =>
                {
                    FinishAffinity();
                });
                return false;
            }

            if (ungrantedOptional.Count > 0)
            {
                Utility.ShowAlert(this, "Warning: Ungranted Permissions", $"The following optional permissions are not granted.\n{string.Join('\n', ungrantedOptional)}", "OK");
            }
            return true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (initializedAndCreated_)
            {
                StopRepeatingTask();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (initializedAndCreated_)
            {
                RefreshAll();
            }
        }

        public override void OnCreateContextMenu(IContextMenu? menu, View? v, IContextMenuContextMenuInfo? menuInfo)
        {
            // https://www.geeksforgeeks.org/context-menu-in-android-with-example/

            base.OnCreateContextMenu(menu, v, menuInfo);

            menu.Add("Send Reminder Message Manually");
            menu.Add("Copy Reminder Message");
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            // https://stackoverflow.com/questions/18632331/using-contextmenu-with-listview-in-android
            AdapterContextMenuInfo info = (AdapterContextMenuInfo)item.MenuInfo;
            var position = info.Position;

            if (position < 0)
            {
                return false;
            }

            var selReminder = reminders_[position];
            if (item.TitleFormatted.ToString() == "Send Reminder Message Manually")
            {
                // chat-gpt generated:
                Intent sendIntent = new Intent(Intent.ActionView);

                // Open SMS app with or without a target phone number.
                var number = selReminder.Contact?.MostLikelyNumber;
                if (number != null)
                {
                    sendIntent.SetData(Android.Net.Uri.Parse("smsto:" + number));
                }
                else
                {
                    sendIntent.SetData(Android.Net.Uri.Parse("sms:"));
                }

                sendIntent.PutExtra("sms_body", selReminder.Message); // Optional: Prepopulate the message body
                StartActivity(sendIntent);
            }
            else if (item.TitleFormatted.ToString() == "Copy Reminder Message")
            {
                this.CopyToClipboard(selReminder.Message);
            }

            return true;
        }

        public void RefreshAll()
        {
            Data.Instance.ReloadContacts(this);

            RefreshReminders();
        }

        private void ValidateSettingsOnFirstRun()
        {
            var warnings = new List<string>();
            var errors = new List<string>();

            foreach (var field in Settings.Instance.Fields)
            {
                var (error, warning) = field.Validate();
                if (!string.IsNullOrEmpty(error))
                {
                    errors.Add(error);
                }
                if (!string.IsNullOrEmpty(warning))
                {
                    warnings.Add(warning);
                }
            }

            if (errors.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("The settings fields have the following errors:");
                foreach (var error in errors)
                {
                    sb.AppendLine(error);
                }
                Utility.ShowAlert(this, "Initial Settings Validation", sb.ToString(), "OK");
            }
            else
            {
                if (warnings.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("The settings fields have the following warnings:");
                    foreach (var warning in warnings)
                    {
                        sb.AppendLine(warning);
                    }
                    Utility.ShowAlert(this, "Initial Settings Validation", sb.ToString(), "OK");
                }
            }
        }

        private void StartRepeatingTask()
        {
            reminderChecker_ = new ReminderChecker(this);
        }

        private void StopRepeatingTask()
        {
            reminderChecker_.StopRepeatingTask();
        }

        private void CheckDelayReminder()
        {
            RefreshReminders();

            if (reminders_.Count > 0)
            {
                const string NotificationText = "Need to review and run daily reminder.";
                var notification = new Notification(Resource.Mipmap.appicon, NotificationText, System.Environment.TickCount);
                PendingIntent contentIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(MainActivity)), 0);
                notification.SetLatestEventInfo(this, "Booking SMS Reminder", NotificationText, contentIntent);
                var nm = (NotificationManager)GetSystemService(NotificationService);
                nm.Notify(0, notification);
            }
        }

        private void ButtonAddBooking_Click(object? sender, EventArgs e)
        {
            Intent switchActivityIntent = new Intent(this, typeof(AddBookingActivity));
            StartActivity(switchActivityIntent);
        }

        private void ButtonSettings_Click(object? sender, EventArgs e)
        {
            var switchActivityIntent = new Intent(this, typeof(SettingsActivity));
            StartActivity(switchActivityIntent);
        }

        private void ButtonDismissSelected_Click(object? sender, EventArgs e)
        {
            Utility.ShowAlert(this, "Dismissing Reminders", "Are you sure you want to dismiss the selected reminders?", "Yes", "No", () =>
            {
                foreach (var reminder in reminders_)
                {
                    if (reminder.Selected)
                    {
                        dismissedRemindersLog_.CleanUpLogAndAddNewEntry((reminder.Contact, reminder.StartTime!.Value));
                    }
                }

                RefreshReminders();
            }, null);
        }

        private void ButtonSendSelected_Click(object? sender, EventArgs e)
        {
            Utility.ShowAlert(this, "Sending Reminders", "Are you sure you want to send off the selected reminders?", "Yes", "No", () =>
            {
                var persons = new List<string>();
                var c = 0;

                foreach (var reminder in reminders_)
                {
                    if (reminder.Selected)
                    {
                        SendMessage(reminder.PhoneNumber, reminder.Message);
                        c++;
                        persons.Add(reminder.Name);

                        sentRemindersLog_.CleanUpLogAndAddNewEntry((reminder.Contact, reminder.StartTime!.Value));
                    }
                }

                var sb = new System.Text.StringBuilder();
                if (c > 0)
                {
                    sb.AppendLine($"Messages have been sent to the following {c} persons:");
                    sb.Append(string.Join(", ", persons));
                    sb.Append(".");
                }
                else
                {
                    sb.AppendLine($"No messages have been sent.");
                }
                Utility.ShowAlert(this, "Messages Sent", sb.ToString(), "OK");

                RefreshReminders();
            }, null);
        }

        private void ButtonSelectNone_Click(object? sender, EventArgs e)
        {
            foreach (var reminder in reminders_)
            {
                reminder.Selected = false;
            }
            remindersAdapter_.NotifyDataSetChanged();
        }

        private void ButtonSelectAll_Click(object? sender, EventArgs e)
        {
            foreach (var reminder in reminders_)
            {
                if (reminder.Status == Reminder.StatusEnum.Pending)
                {
                    reminder.Selected = true;
                }
            }
            remindersAdapter_.NotifyDataSetChanged();
        }

        private void RefreshReminders()
        {
            lock (this)
            {
                var reminders = GenerateReminders();
                reminders_.Clear();
                reminders_.AddRange(reminders);

                remindersAdapter_.NotifyDataSetChanged();
            }
        }

        private void SendMessage(string phone, string message)
        {
            // Define the precompiler to disable SMS for debugging.
#if !SUPPRESS_SENDING_SMS
            var smsManager = SmsManager.Default;

            PendingIntent sentPI;
            const string SENT = "SMS_SENT";

            sentPI = PendingIntent.GetBroadcast(this, 0, new Intent(SENT), 0);

            smsManager.SendTextMessage(phone, null, message, sentPI, null);
#endif
        }

        private IEnumerable<Reminder> GenerateReminders()
        {
            //https://learn.microsoft.com/en-gb/previous-versions/xamarin/android/user-interface/controls/calendar

            var kmpCalId = Utility.GetKmpCalendarId(this);

            if (kmpCalId.HasValue)
            {
                var eventsUri = CalendarContract.Events.ContentUri;

                string[] eventsProjection = {
                    CalendarContract.Events.InterfaceConsts.Id,
                    CalendarContract.Events.InterfaceConsts.Title,
                    CalendarContract.Events.InterfaceConsts.Dtstart,
                };

                var eventLoader = new CursorLoader(this, eventsUri, eventsProjection,
                                    string.Format("calendar_id={0}", kmpCalId.Value), null, "dtstart ASC");

                var eventCursor = (ICursor)eventLoader.LoadInBackground();
                for (var moveSucceeded = eventCursor.MoveToLast(); moveSucceeded; moveSucceeded = eventCursor.MoveToPrevious())
                {
                    var eventTitle = eventCursor.GetString(eventCursor.GetColumnIndex(eventsProjection[1])).Trim();

                    var dtstartMs = eventCursor.GetLong(eventCursor.GetColumnIndex(eventsProjection[2]));
                    // NOTE Make sure the calendar is using local time!
                    var dtStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(dtstartMs).ToLocalTime();

                    if (dtStart < DateTime.Now)
                    {
                        break;
                    }

                    if (IsRemindableStartTime(dtStart))
                    {
                        // assume eventTitle contains the name
                        var clientName = eventTitle;

                        var index = clientName.IndexOf("booking", StringComparison.OrdinalIgnoreCase);
                        if (index >= 0)
                        {
                            clientName = clientName[..index].Trim();
                        }

                        var contact = Utility.SmartFindContact(clientName);

                        var reminderMessage = Utility.GenerateMessage(Settings.Instance, contact, dtStart, null);

                        string name;
                        string? nameInCalendar = null;
                        string? reminderStatusDescription = null;
                        string? phoneNumber = null;

                        Reminder.StatusEnum status;

                        if (contact != null)
                        {
                            name = contact.DisplayName;
                            nameInCalendar = clientName.ToLower() != contact.DisplayName.ToLower() ? clientName : null;
                            phoneNumber = contact.MostLikelyNumber;

                            if (dismissedRemindersLog_.GetIfMessageLogged(contact, dtStart))
                            {
                                status = Reminder.StatusEnum.Dismissed;
                                reminderStatusDescription = $"Reminder for {contact.DisplayName} on {Utility.PrintDateTime(dtStart)} is dismissed.";
                            }
                            else if (sentRemindersLog_.GetIfMessageLogged(contact, dtStart))
                            {
                                status = Reminder.StatusEnum.Sent;
                                reminderStatusDescription = $"Reminder for {contact.DisplayName} on {Utility.PrintDateTime(dtStart)} already sent.";
                            }
                            else if (contact.MostLikelyNumber != null)
                            {
                                status = Reminder.StatusEnum.Pending;
                            }
                            else
                            {
                                status = Reminder.StatusEnum.Error;
                                reminderStatusDescription = $"ERROR: Unable to send message to {clientName} for an appt {Utility.PrintDateTime(dtStart)} since no valid mobile phone number is provided. This reminder needs to be manually handled.";
                            }
                        }
                        else
                        {
                            name = clientName;
                            status = Reminder.StatusEnum.Error;
                            reminderStatusDescription = $"ERROR: Unable to find contact detail for {clientName} for an appt {Utility.PrintDateTime(dtStart)}. This reminder needs to be manually handled.";
                        }

                        yield return new Reminder
                        {
                            Status = status,
                            Selected = false,
                            Name = name,
                            NameInCalendar = nameInCalendar,
                            PhoneNumber = phoneNumber,
                            StatusDescription = reminderStatusDescription,
                            Message = reminderMessage,
                            Contact = contact,
                            StartTime = dtStart
                        };
                    }
                }
            }
        }



        private static bool IsRemindableStartTime(DateTime dtStart)
        {
            return dtStart.Date - DateTime.Now.Date == TimeSpan.FromDays(1);
        }
    }
}
