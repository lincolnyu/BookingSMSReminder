using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Telephony;
using Android.Views;
using Java.Util;

namespace BookingSMSReminder
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        class Reminder
        {
            public bool ToSend;

            public bool Enabled;
            public string? Name;
            public string? PhoneNumber;
            public string Message;

            public Data.Contact? Contact;
            public DateTime? StartTime;

            public override string ToString()
            {
                return Enabled ? $"[{Name}, {PhoneNumber}] {Message}" : Message;
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
                    reminder_.ToSend = isChecked;
                }
            }

            public override View? GetView(int position, View? convertView, ViewGroup? parent)
            {
                ViewHolder? holder;

                if (convertView == null)
                {
                    convertView = inflater_.Inflate(Resource.Layout.activity_custom_row, null);

                    CheckBox cb1 = convertView.FindViewById<CheckBox>(Resource.Id.to_send);

                    holder = new ViewHolder();
                    holder.CheckBox = cb1;
                    convertView.Tag = holder;

                    holder.CheckBox.SetOnCheckedChangeListener(new CheckedChangeListener(reminders_[position]));
                }
                else
                {
                    holder = (ViewHolder)convertView.Tag;
                }

                var reminder = reminders_[position];

                TextView tv = convertView.FindViewById<TextView>(Resource.Id.message);
                tv.Text = reminder.ToString();

                CheckBox cb = convertView.FindViewById<CheckBox>(Resource.Id.to_send);

                if (reminder.Enabled)
                {
                    cb.Enabled = true;
                    cb.Checked = reminder.ToSend;
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

                var newThread = new Thread(new ThreadStart(Run));
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
                    var dailyRunTime = context_.DailyRunTime;
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

        private ListView? listViewReminders_;

        private ReminderAdapter remindersAdapter_;
        private List<Reminder> reminders_ = new List<Reminder>();

        private Handler handler_;
        private ReminderChecker reminderChecker_;

        public TimeSpan DailyRunTime = new TimeSpan(22, 28, 0);

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            CleanUpSentMessageDataFileAndAddNewEntry(null);

            listViewReminders_ = FindViewById<ListView>(Resource.Id.listview_reminders);

            remindersAdapter_ = new ReminderAdapter(this, reminders_);
            listViewReminders_.Adapter = remindersAdapter_;

            remindersAdapter_.NotifyDataSetChanged();

            var buttonSelectAll = FindViewById<Button>(Resource.Id.button_select_all);
            buttonSelectAll.Click += ButtonSelectAll_Click;

            var buttonSelectNone = FindViewById<Button>(Resource.Id.button_select_none);
            buttonSelectNone.Click += ButtonSelectNone_Click;

            var buttonSendSelected = FindViewById<Button>(Resource.Id.button_send_selected);
            buttonSendSelected.Click += ButtonSendSelected_Click;

            var buttonAddBooking = FindViewById<Button>(Resource.Id.button_add_booking);
            buttonAddBooking.Click += ButtonAddBooking_Click;

            Data.Instance.ReloadContacts(this);

            handler_ = new Handler();

            StartRepeatingTask();
        }

        
        protected override void OnDestroy()
        {
            base.OnDestroy();

            StopRepeatingTask();
        }

        protected override void OnResume()
        {
            base.OnResume();

            Data.Instance.ReloadContacts(this);
            RefreshReminders();
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

        private void ButtonSendSelected_Click(object? sender, EventArgs e)
        {
            var persons = new List<string>();
            var c = 0;
            foreach (var reminder in reminders_)
            {
                if (reminder.ToSend)
                {
                    SendMessage(reminder.PhoneNumber, reminder.Message);

                    c++;
                    persons.Add(reminder.Name);
                    CleanUpSentMessageDataFileAndAddNewEntry((reminder.Contact, reminder.StartTime!.Value));
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
            Utility.ShowAlert(this, "Message Sent", sb.ToString(), "OK");

            RefreshReminders();
        }

        private void ButtonSelectNone_Click(object? sender, EventArgs e)
        {
            foreach (var reminder in reminders_)
            {
                reminder.ToSend = false;
            }
            remindersAdapter_.NotifyDataSetChanged();
        }

        private void ButtonSelectAll_Click(object? sender, EventArgs e)
        {
            foreach (var reminder in reminders_)
            {
                if (reminder.Enabled)
                {
                    reminder.ToSend = true;
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
            var smsManager = SmsManager.Default;

            PendingIntent sentPI;
            const string SENT = "SMS_SENT";

            sentPI = PendingIntent.GetBroadcast(this, 0, new Intent(SENT), 0);

            smsManager.SendTextMessage(phone, null, message, sentPI, null);
        }

        
        
        private IEnumerable<Reminder> GenerateReminders()
        {
            //https://learn.microsoft.com/en-gb/previous-versions/xamarin/android/user-interface/controls/calendar

            var calendarsUri = CalendarContract.Calendars.ContentUri;

            string[] calendarsProjection = {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
                CalendarContract.Calendars.InterfaceConsts.AccountName
            };

            var loader = new CursorLoader(this, calendarsUri, calendarsProjection, null, null, null);
            var cursor = (ICursor)loader.LoadInBackground();

            int? kmpCalId = null;
            bool moveSucceeded = false;
            for (moveSucceeded = cursor.MoveToFirst(); moveSucceeded; moveSucceeded = cursor.MoveToNext())
            {
                int calId = cursor.GetInt(cursor.GetColumnIndex(calendarsProjection[0]));
                string calDisplayName = cursor.GetString(cursor.GetColumnIndex(calendarsProjection[1]));
                string calAccountName = cursor.GetString(cursor.GetColumnIndex(calendarsProjection[2]));
                if (calAccountName == "kineticmobilept@gmail.com" && calDisplayName == "kineticmobilept@gmail.com")
                {
                    kmpCalId = calId;
                    break;
                }
            }

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
                for (moveSucceeded = eventCursor.MoveToLast(); moveSucceeded; moveSucceeded = eventCursor.MoveToPrevious())
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

                        if (Data.Instance.Contacts.TryGetValue(clientName.ToLower(), out var contact))
                        {
                            if (!GetIfMessageSent(contact, dtStart))
                            {
                                if (contact.MostLikelyNumber != null)
                                {
                                    yield return new Reminder
                                    {
                                        Enabled = true,
                                        ToSend = true,
                                        Name = contact.DisplayName,
                                        PhoneNumber = contact.MostLikelyNumber,
                                        Message = $"Appointment reminder for {PrintDateTime(dtStart)} with Cibo Cai at Kinetic Mobile Physio. Please reply Y to confirm or call 0400693696 to reschedule. Thanks.",
                                        Contact = contact,
                                        StartTime = dtStart
                                    };
                                }
                                else
                                {
                                    yield return new Reminder
                                    {
                                        Enabled = false,
                                        ToSend = false,
                                        Name = contact.DisplayName,
                                        PhoneNumber = contact.MostLikelyNumber,
                                        Message = $"ERROR: Unable to send message to {clientName} with an appt {PrintDateTime(dtStart)} since no valid mobile phone number is provided. This reminder needs to be manually handled.",
                                        Contact = contact,
                                        StartTime = dtStart
                                    };
                                }
                            }
                            else
                            {
                                yield return new Reminder
                                {
                                    Enabled = false,
                                    ToSend = false,
                                    Name = contact.DisplayName,
                                    PhoneNumber = contact.MostLikelyNumber,
                                    Message = $"Reminder for {contact.DisplayName} on {PrintDateTime(dtStart)} already sent.",
                                    Contact = contact,
                                    StartTime = dtStart
                                };
                            }
                        }
                        else
                        {
                            yield return new Reminder
                            {
                                Enabled = false,
                                ToSend = false,
                                Name = clientName,
                                Message = $"ERROR: Unable to find contact detail for {clientName} with an appointment at {dtStart.ToShortTimeString()} on {dtStart.ToLongDateString()}. This reminder needs to be manually handled."
                            };
                        }
                    }
                }
            }
        }

        private string PrintDateTime(DateTime dtStart)
        {
            string[] Months = [ "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" ];
            string[] DaysOfWeek = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

            var timeStr = Utility.PrintTime(dtStart.Hour, dtStart.Minute);

            var day = dtStart.Day;
            var month = Months[dtStart.Month-1];
            var year = dtStart.Year;
            var dayOfWeek = DaysOfWeek[(int)dtStart.DayOfWeek];
            return $"{dayOfWeek} {day} {month} {year} @ {timeStr}";
        }

        private bool GetIfMessageSent(Data.Contact contact, DateTime startTime)
        {
            var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var sentMessagesDataFile = Path.Combine(appDataPath, "sent_messages.log");
            if (!File.Exists(sentMessagesDataFile))
            {
                return false;
            }
            using var sr = new StreamReader(sentMessagesDataFile);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;
                var segs = line.Split('|');
                if (segs.Length != 2) continue;
                var number = segs[0];
                var stt = DateTime.Parse(segs[1]);
                if (number == contact.MostLikelyNumber && stt == startTime)
                {
                    return true;
                }
            }
            return false;
        }

        private void CleanUpSentMessageDataFileAndAddNewEntry((Data.Contact, DateTime)? newEntry)
        {
            var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var sentMessagesDataFile = Path.Combine(appDataPath, "sent_messages.log");
            bool newEntryAlreadyExists = false;
            var lines = new List<string>();
            if (File.Exists(sentMessagesDataFile))
            {
                {
                    using var sr = new StreamReader(sentMessagesDataFile);
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
                using var sw = new StreamWriter(sentMessagesDataFile);
                foreach (var line in lines)
                {
                    sw.WriteLine(line);
                }
                if (newEntry.HasValue && !newEntryAlreadyExists)
                {
                    sw.WriteLine($"{newEntry.Value.Item1.MostLikelyNumber}|{newEntry.Value.Item2}");
                }
            }

        }

        private bool IsRemindableStartTime(DateTime dtStart)
        {
            return dtStart.Date - DateTime.Now.Date == TimeSpan.FromDays(1);
        }

        private bool IsDatePastForReminding(DateTime dtStart)
        {
            return dtStart.Date - DateTime.Now.Date < TimeSpan.FromDays(1);
        }

        private void SetUpDailyAlarm()
        {
            Calendar calendar = Calendar.Instance;

            calendar.Set(CalendarField.HourOfDay, 21);
            calendar.Set(CalendarField.Minute, 0);
            calendar.Set(CalendarField.Second, 0);

            PendingIntent pi = PendingIntent.GetService(this, 0,
            new Intent(this, typeof(MainActivity)), PendingIntentFlags.UpdateCurrent);
            AlarmManager am = (AlarmManager)GetSystemService(Context.AlarmService);
            am.SetRepeating(AlarmType.RtcWakeup, calendar.TimeInMillis, AlarmManager.IntervalDay, pi);
        }
    }
}
