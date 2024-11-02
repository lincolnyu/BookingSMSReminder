using Android.Content;
using Android.Database;
using Android.Provider;
using System.Text;
using System.Text.RegularExpressions;
using static BookingSMSReminder.Data;
using static BookingSMSReminder.Settings;

namespace BookingSMSReminder
{
    public static class Utility
    {
        public const int MaxAllowedSMSLength = 160;

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

        public static int? GetCalendarId(Settings settings, Context context)
        {
            var calendarsUri = CalendarContract.Calendars.ContentUri;

            string[] calendarsProjection = {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
                CalendarContract.Calendars.InterfaceConsts.AccountName
            };

            // If they are null (specified) no matching calendar ID will be found and null will be returned.
            var configuredCalendarAccountName = ((Field<string>)settings.Fields[Settings.FieldIndex.CalendarAccountName]).Value;
            var configuredCalendarDisplayName = ((Field<string>)settings.Fields[Settings.FieldIndex.CalendarDisplayName]).Value;

            var loader = new CursorLoader(context, calendarsUri, calendarsProjection, null, null, null);
            var cursor = (ICursor)loader.LoadInBackground();

            bool moveSucceeded = false;
            for (moveSucceeded = cursor.MoveToFirst(); moveSucceeded; moveSucceeded = cursor.MoveToNext())
            {
                int calId = cursor.GetInt(cursor.GetColumnIndex(calendarsProjection[0]));
                string calDisplayName = cursor.GetString(cursor.GetColumnIndex(calendarsProjection[1]));
                string calAccountName = cursor.GetString(cursor.GetColumnIndex(calendarsProjection[2]));
                if (calAccountName == configuredCalendarAccountName && calDisplayName == configuredCalendarDisplayName)
                {
                    return calId;
                }
            }
            return null;
        }

        public static TimeOnly GetDailyNotificationTime(Settings settings)
        {
            var field = (Settings.Field<TimeOnly>)settings.Fields[Settings.FieldIndex.DailyNotificationTime];
            return field.Value;
        }

        /// <summary>
        ///  Returns the match if contact list contains one and only one name of which all the words cover the split words of <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name to find contat </param>
        /// <returns>The match</returns>
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

        public static string GenerateMessage(Settings settings, Contact? contact, DateTime startDateTime, List<string>? settingsErrors)
        {
            var messageTemplateField = (Field<string>)settings.Fields[Settings.FieldIndex.MessageTemplate];
            var messageTemplate = messageTemplateField.Value;

            if (string.IsNullOrWhiteSpace(messageTemplate)) return "";

            var message = messageTemplate.Replace("<time>", PrintDateTime(startDateTime));
            if (message.Contains("<consultant>"))
            {
                var consultantNameField = (Field<string>)settings.Fields[Settings.FieldIndex.ConsultantName];
                var consultantName = consultantNameField.Value;

                if (string.IsNullOrWhiteSpace(consultantName))
                {
                    if (settingsErrors != null)
                    {
                        settingsErrors.Add("Missing consultant name.");
                    }
                    // The placeholder won't be replaced if there is an error. Same below.
                }
                else
                {
                    message = message.Replace("<consultant>", consultantName);
                }
            }

            if (message.Contains("<organization"))
            {
                var organizationNameField = (Field<string>)settings.Fields[Settings.FieldIndex.OrganizationName];
                var organizationName = organizationNameField.Value;

                if (string.IsNullOrWhiteSpace(organizationName))
                {
                    if (settingsErrors != null)
                    {
                        settingsErrors.Add("Missing organization name.");
                    }
                }
                else
                {
                    message = message.Replace("<organization>", organizationName);
                }
            }

            if (message.Contains("<phone>"))
            {
                var organizationPhoneField = (Field<string>)settings.Fields[Settings.FieldIndex.OrganizationPhone];
                var organizationPhone = organizationPhoneField.Value;

                if (string.IsNullOrWhiteSpace(organizationPhone))
                {
                    if (settingsErrors != null)
                    {
                        settingsErrors.Add("Missing organization phone number.");
                    }
                }
                else
                {
                    message = message.Replace("<phone>", organizationPhone);
                }
            }

            if (message.Contains("<client>"))
            {
                var clientName = contact?.DisplayName;
                // Because missing client name is not a settings error, so we don't report.
                if (!string.IsNullOrWhiteSpace(clientName))
                {
                    message = message.Replace("<client>", clientName);
                }
            }

            return message;
        }

        /// <summary>
        ///  Validate message template against other settings and returns non-empty string if validation fails.
        /// </summary>
        /// <param name="settings">The settings in which message template is validated.</param>
        /// <returns>Non-empty string if there are validation errors.</returns>
        public static string ValidateMessageTemplate(Settings settings)
        {
            var dummyContact = new Contact("David Smiths");
            var errors = new List<string>();
            var message = GenerateMessage(settings, dummyContact, new DateTime(2000, 12, 31, 18, 30, 30), errors);
            if (message.Length > MaxAllowedSMSLength)
            {
                errors.Insert(0, "Message may exceed 160 character limit.");
            }
            return string.Join(" ", errors);
        }

        public static string GenerateEventTitle(Settings settings, string clientName)
        {
            var appAddedEventTitle = ((Settings.Field<string>)settings.Fields[Settings.FieldIndex.AppAddedEventTitle]).Value ?? "<client>";
            return appAddedEventTitle.Replace("<client>", clientName);
        }

        /// <summary>
        ///  Extract client name from event title.
        /// </summary>
        /// <param name="eventTitle">The event title to extract client name from</param>
        /// <param name="regexList">Regex patterns ordered by precedence</param>
        /// <returns>The client name if found or null</returns>
        public static string? ParseClientName(string eventTitle, Regex[] regexList)
        {
            foreach (var regex in regexList)
            {
                var m = regex.Match(eventTitle);
                if (m.Success && m.Value == eventTitle)
                {
                    if (m.Groups[1].Success)
                    {
                        return m.Groups[1].Value;
                    }
                }
            }
            return null;
        }

        public static string EventTitleFormatToRegexPattern(string eventTitleFormat, bool wasRegex)
        {
            var pattern = eventTitleFormat;
            if (!wasRegex)
            {
                char[] RegexReservedChars = ['[', ']', '(', ')', '.', '*', '+'];
                foreach (var c in RegexReservedChars)
                {
                    pattern = pattern.Replace($"{c}", @"\" + c);
                }
            }

            const string ClientNameUnit = "[A-Za-z][A-Za-z0-9'\\-\\.]*[ ,]*";
            const int MaxClientNameUnitsCount = 5;
            var sbClientPattern = new StringBuilder("(");

            for (var i = 1; i <= MaxClientNameUnitsCount; i++)
            {
                if (i != 1)
                {
                    sbClientPattern.Append("|");
                }
                for (var j = 0; j < i; j++)
                {
                    sbClientPattern.Append(ClientNameUnit);
                }
            }
            sbClientPattern.Append(")");

            pattern = pattern.Replace("<client>", sbClientPattern.ToString());

            return pattern;
        }
    }
}
