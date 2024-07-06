﻿using Android.Content;
using Android.Database;
using Android.Provider;

namespace BookingSMSReminder
{
    public static class Utility
    {
        public static string PrintTime(int hourOfDay, int minute)
        {
            var hour = hourOfDay > 12 ? hourOfDay - 12 : hourOfDay;
            var ampm = hourOfDay > 12 ? "pm" : "am";

            return $"{hour}:{minute:00}{ampm}";
        }

        public static void ShowAlert(Context context, string title, string message, string okButtonText)
        {
            // Create a builder object that builds the AlertDialog.
            var builder = new AlertDialog.Builder(context);

            // Set the message shown for the Alert.
            builder.SetMessage(message);

            // Set Alert Title.
            builder.SetTitle(title);

            // Set Cancelable false for when the user clicks on the outside the Dialog Box then it will remain showing.
            builder.SetCancelable(false);

            // Set the positive button with the specified caption. Lambda OnClickListener doesn't need to do anything.
            builder.SetPositiveButton(okButtonText, (sender, args) => {
            });

            builder.Show();
        }

        public static int? GetKmpCalendarId(Context context)
        {
            var calendarsUri = CalendarContract.Calendars.ContentUri;

            string[] calendarsProjection = {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
                CalendarContract.Calendars.InterfaceConsts.AccountName
            };

            var loader = new CursorLoader(context, calendarsUri, calendarsProjection, null, null, null);
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
                    return calId;
                }
            }
            return null;
        }
    }
}
