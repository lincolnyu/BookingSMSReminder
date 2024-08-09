using Android.Content;
using System.Text;

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

            var buttonReset = FindViewById<Button>(Resource.Id.button_reset_settings);
            buttonReset.Click += EditToDefaultReset_Click;

            UpdateConfigToEditTexts();
        }

        protected override void OnPause()
        {
            base.OnPause();

            UpdateEditTextsToConfig(false, null);
        }

        private void ButtonBackToMain_Click(object? sender, EventArgs e)
        {
            UpdateEditTextsToConfig(true, () => ReturnToMain());
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
                var field = Settings.Instance.Fields[Settings.FieldIndex.DailyNotificationTime];
                field.UpdateToConfig(ntToSet);
                field.UpdateConfigToUI(this);
            };

            var notificationTime = Utility.GetDailyNotificationTime();
            var timePickerDialog = new TimePickerDialog(this, handler, notificationTime.Hour, notificationTime.Minute, false);
            timePickerDialog.Show();
        }

        private void UpdateConfigToEditTexts()
        {
            foreach (var field in Settings.Instance.Fields)
            {
                field.UpdateConfigToUI(this);
            }
        }

        private void UpdateEditTextsToConfig(bool interactive, Action? endAction)
        {
            foreach (var field in Settings.Instance.Fields)
            {
                if (field.EditorResourceId.HasValue)
                {
                    var editText = FindViewById<EditText>(field.EditorResourceId.Value);
                    var (val, succ) = field.ConvertUIStringToValue(editText.Text.Trim());
                    if (succ)
                    {
                        field.UpdateToConfig(val);
                    }
                }
            }

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
                Config.Instance.Reload();   // Revert changes.
                if (interactive)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("The settings fields have the following errors, and the config changes are not saved.");
                    foreach (var error in errors)
                    {
                        sb.AppendLine(error);
                    }
                    Utility.ShowAlert(this, "Settings Errors", sb.ToString(), "OK", endAction);
                    return;
                }
            }
            else 
            {
                Config.Instance.Save();     // Persist the changes when there's no errors.
                if (warnings.Count > 0)
                {
                    if (interactive)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("The settings fields have the following warnings, but the changes are saved.");
                        foreach (var warning in warnings)
                        {
                            sb.AppendLine(warning);
                        }
                        Utility.ShowAlert(this, "Settings Warnings", sb.ToString(), "OK", endAction);
                        return;
                    }
                }
            }
            endAction?.Invoke();
        }

        private void EditToDefaultReset_Click(object? sender, EventArgs e)
        {
            foreach (var field in Settings.Instance.Fields)
            {
                if (field.EditorResourceId.HasValue)
                {
                    Config.Instance.ClearValue(field.ConfigField);
                    field.UpdateConfigToUI(this);
                }
            }
        }
    }
}
