﻿
using Android.Content;
using Android.Text.Format;
using Java.Text;
using Java.Util;

namespace BookingSMSReminder
{
    [Activity()]
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

            var clients = Data.Instance.Contacts.Keys;

            var adapter = new ArrayAdapter<string>(this, Resource.Layout.activity_client_suggestion, Resource.Id.select_client_name, clients.ToArray());
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
            Intent switchActivityIntent = new Intent(this, typeof(MainActivity));
            StartActivity(switchActivityIntent);
        }

        private void ButtonConfirmAdd_Click(object? sender, EventArgs e)
        {
            // TODO add to calednar.
            throw new NotImplementedException();
        }
    }
}