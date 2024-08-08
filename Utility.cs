using Android.Content;
using Android.Database;
using Android.Provider;
using Android.Service.VR;
using static BookingSMSReminder.Data;
using static BookingSMSReminder.Settings;

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

        public static void CopyToClipboard(this Context context, string text)
        {
            ClipboardManager clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);
            ClipData clip = ClipData.NewPlainText("Copied Text", text);
            clipboard.PrimaryClip = clip;
        }


        public static void ShowAlert(Context context, string title, string message, string okButtonText, Action? action = null)
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
                action?.Invoke();
            });

            builder.Show();
        }

        public static void ShowAlert(Context context, string title, string message, string positiveButtonText, string negativeButtonText, Action? positiveAction, Action? negativeAction,
            bool cancellable = true)
        {
            // Create a builder object that builds the AlertDialog.
            var builder = new AlertDialog.Builder(context);

            // Set the message shown for the Alert.
            builder.SetMessage(message);

            // Set Alert Title.
            builder.SetTitle(title);

            // Set Cancelable false for when the user clicks on the outside the Dialog Box then it will remain showing.
            builder.SetCancelable(cancellable);

            // Set the positive button with the specified caption. Lambda OnClickListener doesn't need to do anything.
            builder.SetPositiveButton(positiveButtonText, (sender, args) => {
                positiveAction?.Invoke();
            });

            builder.SetNegativeButton(negativeButtonText, (sender, args) => {
                negativeAction?.Invoke();
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

        public static TimeOnly GetDailyNotificationTime()
        {
            var field = (Settings.Field<TimeOnly>)Settings.Instance.Fields[Settings.FieldIndex.DailyNotificationTime];
            return field.Value;
        }

        public static Contact? SmartFindContact(string name)
        {
            var nameLower = name.ToLower();
            if (Data.Instance.Contacts.TryGetValue(nameLower, out var exactMatch))
            {
                return exactMatch;
            }

            var nameSplit = nameLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var foundMatches = new List<Contact>();
            foreach (var contact in Data.Instance.Contacts.Values)
            {
                var split = contact.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLower());
                var match = true;
                foreach (var s in nameSplit)
                {
                    if (!split.Contains(s))
                    {
                        match = false;
                    }
                }
                if (match)
                {
                    foundMatches.Add(contact);
                }
            }
            if (foundMatches.Count == 1)
            {
                return foundMatches[0];
            }
            return null;
        }

        public static string PrintDateTime(DateTime dtStart)
        {
            string[] Months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
            string[] DaysOfWeek = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

            var timeStr = Utility.PrintTime(dtStart.Hour, dtStart.Minute);

            var day = dtStart.Day;
            var month = Months[dtStart.Month - 1];
            var year = dtStart.Year;
            var dayOfWeek = DaysOfWeek[(int)dtStart.DayOfWeek];
            return $"{dayOfWeek} {day} {month} {year} @ {timeStr}";
        }

        public static string GenerateMessage(Settings settings, Contact? contact, DateTime startDateTime)
        {
            var messageTemplateField= (Field<string>)settings.Fields[Settings.FieldIndex.MessageTemplate];
            var messageTemplate = messageTemplateField.Value;

            if (string.IsNullOrWhiteSpace(messageTemplate)) return "";

            var consultantNameField = (Field<string>)settings.Fields[Settings.FieldIndex.ConsultantName];
            var consultantName = consultantNameField.Value;

            var organizationNameField = (Field<string>)settings.Fields[Settings.FieldIndex.OrganizationName];
            var organizationName = organizationNameField.Value;

            var organizationPhoneField = (Field<string>)settings.Fields[Settings.FieldIndex.OrganizationPhone];
            var organizationPhone = organizationPhoneField.Value;

            var clientName = contact?.DisplayName?? "";

            var message = messageTemplate.Replace("<time>", PrintDateTime(startDateTime))
                .Replace("<consultant>", consultantName)
                .Replace("<organization>", organizationName)
                .Replace("<phone>", organizationPhone)
                .Replace("<client>", clientName);

            return message;
        }

        public static int EvaluateMessageLength(Settings settings)
        {
            var dummyContact = new Contact("David Smiths");
            return GenerateMessage(settings, dummyContact, new DateTime(2000,12,31, 18, 30, 30)).Length;
        }
    }
}
