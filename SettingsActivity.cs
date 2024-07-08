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

            var buttonReset = FindViewById<Button>(Resource.Id.button_reset_sent_messages_log);
            buttonReset.Click += ButtonResetSentMessagesLog_Click;

            var editNotificationTime = FindViewById<EditText>(Resource.Id.edit_notification_time);
            editNotificationTime.Click += EditNotificationTime_Click;
            UpdateEditTime();
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
            Utility.ShowAlert(this, "Resetting Sent Messages Log", "Are you sure you want to reset the Sent Messages Log?", "Yes", "No",
                () => {
                    var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                    var sentMessagesDataFile = Path.Combine(appDataPath, "sent_messages.log");
                    File.Delete(sentMessagesDataFile);
                }, null, true);
            // Main activity when switched back will call RefreshAll in OnResune()
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
    }
}
