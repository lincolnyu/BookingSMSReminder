using Android.Content;

namespace BookingSMSReminder
{
    [Activity]
    public class SettingsActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_settings);

            var buttonBackToMain = FindViewById<Button>(Resource.Id.button_settings_back_to_main);
            buttonBackToMain.Click += ButtonBackToMain_Click;

            var buttonResetSent = FindViewById<Button>(Resource.Id.button_reset_sent_reminders_log);
            buttonResetSent.Click += ButtonResetSentMessagesLog_Click;

            var buttonResetDismissed = FindViewById<Button>(Resource.Id.button_reset_dismissed_reminders_log);
            buttonResetDismissed.Click += ButtonResetDismissedMessagesLog_Click;

            var editNotificationTime = FindViewById<EditText>(Resource.Id.edit_notification_time);
            editNotificationTime.Click += EditNotificationTime_Click;
            UpdateEditTime();

            UpdatePractitionerName();

            UpdateOrganizationName();
        }

        
        protected override void OnPause()
        {
            base.OnPause();

            UpdateEditTextsToConfig();
        }

        private void ButtonBackToMain_Click(object? sender, EventArgs e)
        {
            ReturnToMain();
        }

        private void ReturnToMain()
        {
            Intent switchActivityIntent = new Intent(this, typeof(MainActivity));
            StartActivity(switchActivityIntent);
        }

        private void ButtonResetSentMessagesLog_Click(object? sender, EventArgs e)
        {
            Utility.ShowAlert(this, "Resetting Sent Reminders Log", "Are you sure you want to reset the Sent Reminders Log?", "Yes", "No",
                () => {
                    var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                    var sentRemindersDataFile = Path.Combine(appDataPath, "sent_reminders.log");
                    File.Delete(sentRemindersDataFile);
                }, null, true);
            // Main activity when switched back will call RefreshAll in OnResune()
        }

        private void ButtonResetDismissedMessagesLog_Click(object? sender, EventArgs e)
        {
            Utility.ShowAlert(this, "Resetting Dismissed Reminders Log", "Are you sure you want to reset the Dismissed Reminders Log?", "Yes", "No",
                () => {
                    var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                    var dismissedRemindersDataFile = Path.Combine(appDataPath, "dismissed_reminders.log");
                    File.Delete(dismissedRemindersDataFile);
                }, null, true);
        }


        private void EditNotificationTime_Click(object? sender, EventArgs e)
        {
            EventHandler<TimePickerDialog.TimeSetEventArgs> handler = (sender, args) =>
            {
                var ntToSet = new TimeOnly(args.HourOfDay, args.Minute);
                Config.Instance.SetValue("daily_notification_time", ntToSet.ToShortTimeString());
                UpdateEditTime();
            };

            var notificationTime = Utility.GetDailyNotificationTime();
            var timePickerDialog = new TimePickerDialog(this, handler, notificationTime.Hour, notificationTime.Minute, false);
            timePickerDialog.Show();
        }

        private void UpdateEditTime()
        {
            var editText = FindViewById<EditText>(Resource.Id.edit_notification_time);
            var notificationTime = Utility.GetDailyNotificationTime();
            editText.Text = Utility.PrintTime(notificationTime.Hour, notificationTime.Minute);
        }

        private void UpdatePractitionerName()
        {
            var editText = FindViewById<EditText>(Resource.Id.edit_practitioner_name);
            var practitionerName = Config.Instance.GetValue("practitioner_name");
            editText.Text = practitionerName ?? "";
        }

        private void UpdateOrganizationName()
        {
            var editText = FindViewById<EditText>(Resource.Id.edit_organization_name);
            var organizationName = Config.Instance.GetValue("organization_name");
            editText.Text = organizationName ?? "";
        }

        private void UpdateEditTextsToConfig()
        {
            var editPractionerName = FindViewById<EditText>(Resource.Id.edit_practitioner_name);
            Config.Instance.SetValue("practitioner_name", editPractionerName.Text.Trim());

            var editOrganizationName = FindViewById<EditText>(Resource.Id.edit_organization_name);
            Config.Instance.SetValue("organization_name", editOrganizationName.Text.Trim());
        }
    }
}
