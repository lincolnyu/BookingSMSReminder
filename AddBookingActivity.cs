using Android.Content;
using Java.Text;
using Java.Util;

namespace BookingSMSReminder
{
    [Activity]
    public class AddBookingActivity : Activity
    {
        Calendar calendar_ = Calendar.Instance;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_add_booking);

            var buttonBackToMain = FindViewById<Button>(Resource.Id.button_back_to_main);
            buttonBackToMain.Click += ButtonBackToMain_Click;

            var buttonConfirmAdd = FindViewById<Button>(Resource.Id.button_confirm_adding);
            buttonConfirmAdd.Click += ButtonConfirmAdd_Click;

            // https://stackoverflow.com/questions/31052436/android-edittext-with-drop-down-list

            var clients = Data.Instance.Contacts.Values.Select(x=>x.DisplayName).ToArray();

            var adapter = new ArrayAdapter<string>(this, Resource.Layout.activity_client_suggestion, Resource.Id.select_client_name, clients);
            //Find TextView control
            AutoCompleteTextView acTextView = FindViewById<AutoCompleteTextView>(Resource.Id.text_client);
            //Set the number of characters the user must type before the drop down list is shown
            acTextView.Threshold = 1;
            //Set the adapter
            acTextView.Adapter = adapter;

            var editDate = FindViewById<EditText>(Resource.Id.appt_date);
            editDate.Click += EditDate_Click;

            var editTime = FindViewById<EditText>(Resource.Id.appt_time);
            editTime.Click += EditTime_Click;
        }

        protected override void OnResume()
        {
            base.OnResume();

            Data.Instance.ReloadContacts(this);
        }

        private void EditTime_Click(object? sender, EventArgs e)
        {
            EventHandler<TimePickerDialog.TimeSetEventArgs> handler = (sender, args) =>
            {
                calendar_.Set(CalendarField.HourOfDay, args.HourOfDay);
                calendar_.Set(CalendarField.Minute, args.Minute);
                UpdateEditTime();
            };
            var timePickerDialog = new TimePickerDialog(this, handler, calendar_.Get(CalendarField.HourOfDay), calendar_.Get(CalendarField.Minute), false);
            timePickerDialog.Show();
        }

        private void EditDate_Click(object? sender, EventArgs e)
        {
            EventHandler<DatePickerDialog.DateSetEventArgs> handler = (sender, args) =>
            {
                calendar_.Set(CalendarField.Year, args.Year);
                calendar_.Set(CalendarField.Month, args.Month);
                calendar_.Set(CalendarField.DayOfMonth, args.DayOfMonth);
                UpdateEditDate();
            };

            var datePickerDialog = new DatePickerDialog(this, handler, calendar_.Get(CalendarField.Year), calendar_.Get(CalendarField.Month), calendar_.Get(CalendarField.DayOfMonth));
            datePickerDialog.Show();
        }

        private void UpdateEditTime()
        {
            var editText = FindViewById<EditText>(Resource.Id.appt_time);
            editText.Text = Utility.PrintTime(calendar_.Get(CalendarField.HourOfDay), calendar_.Get(CalendarField.Minute));
        }

        private void UpdateEditDate()
        {
            string myFormat = "dd/MM/yy";
            SimpleDateFormat dateFormat = new SimpleDateFormat(myFormat, Locale.Uk);

            var editText = FindViewById<EditText>(Resource.Id.appt_date);
            editText.Text = dateFormat.Format(calendar_.Time);
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

        private void ClearClientName()
        {
            AutoCompleteTextView acTextView = FindViewById<AutoCompleteTextView>(Resource.Id.text_client);
            acTextView.Text = "";
        }

        private void ButtonConfirmAdd_Click(object? sender, EventArgs e)
        {
            var clientText = FindViewById<EditText>(Resource.Id.text_client);
            var client = clientText.Text;
            if (string.IsNullOrEmpty(client))
            {
                Utility.ShowAlert(this, "Error", "Client not specified.", "OK");
                return;
            }

            var durationText = FindViewById<EditText>(Resource.Id.appt_duration);
            if (!int.TryParse(durationText.Text, out var durationMins))
            {
                Utility.ShowAlert(this, "Error", "Invlaid duration value.", "OK");
                return;
            }
            int durationMs = durationMins * 60 * 1000;

            Intent intent = new Intent(Intent.ActionEdit);
            intent.SetType("vnd.android.cursor.item/event");
            intent.PutExtra("beginTime", calendar_.TimeInMillis);
            intent.PutExtra("allDay", false);
            intent.PutExtra("endTime", calendar_.TimeInMillis + durationMs);
            intent.PutExtra("title", $"{client} booking");
            StartActivity(intent);

            // This will open up a new event dialog from Calendar app.
            // Can't return to main here as this will interrupt the event adding.
            // Just clear the form.

            ClearClientName();
        }
    }
}
 