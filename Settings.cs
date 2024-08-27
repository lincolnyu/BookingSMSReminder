namespace BookingSMSReminder
{
    public class Settings
    {
        public interface IField
        {
            string ConfigField { get; }
            int? EditorResourceId { get; }

            void UpdateToUI(Activity activity);

            /// <summary>
            ///  Updates the field value to config with validation
            /// </summary>
            /// <param name="value">The value to convert</param>
            /// <returns>Error string if there's an error.</returns>
            void SetValue(object? value);

            /// <summary>
            ///  Return non-empty strings for validation errors and warnings respectively.
            /// </summary>
            (string, string) Validate();

            (object?, bool) ConvertUIStringToValue(string str);
        }

        public class Field<T> : IField
        {
            public string ConfigField { get; set; }

            public int? EditorResourceId { get; set; }

            /// <summary>
            ///  Null when none is provided.
            /// </summary>
            public T? DefaultValue;

            public Func<T, string>? ValueToUIStringFunc;
            public Func<T, string>? ValueToConfigStringFunc;

            /// <summary>
            ///  Return non-empty strings for validation errors and warnings respectively.
            /// </summary>
            public Func<T?, (string, string)>? ValidateFunc;

            // Returns null upon parse failure
            public Func<string, (T, bool)>? UIStringToValueFunc;
            public Func<string, (T, bool)>? ConfigStringToValueFunc;

            /// <summary>
            ///  Load the value from the config or fall back on the default
            /// </summary>
            public T? Value
            {
                get
                {
                    var str = Config.Instance.GetValue(ConfigField);
                    if (ConfigStringToValueFunc != null && str != null)
                    {
                        var (v, succ) = ConfigStringToValueFunc(str);
                        if (succ)
                        {
                            return v;
                        }
                    }
                    else if (typeof(T) == typeof(string) && str != null)
                    {
                        return (T)((object)str);
                    }
                    return DefaultValue?? default;
                }

                set
                {
                    SetValue(value);
                }
            }

            /// <summary>
            ///  Convert UI string to the represented value using UIStringToValue. If UIStringToValue is unavailable, the UI string is retured as is.
            /// </summary>
            /// <param name="str">The string corresponding to the value in the UI</param>
            /// <returns>The value and whether it exits</returns>
            public (object?, bool) ConvertUIStringToValue(string str)
            {
                if (UIStringToValueFunc != null)
                {
                    var (val, succ) = UIStringToValueFunc(str);
                    return (val, succ);
                }
                else
                {
                    return (str, true);
                }
            }

            public void UpdateToUI(Activity activity)
            {
                if (EditorResourceId != null)
                {
                    var editText = activity.FindViewById<EditText>(EditorResourceId.Value);
                    var val = Value;
                    string uistr;
                    if (ValueToUIStringFunc != null)
                    {
                        uistr = ValueToUIStringFunc(val);
                    }
                    else
                    {
                        uistr = val?.ToString()?? "";
                    }
                    editText.Text = uistr;
                }
            }

            void IField.SetValue(object? value)
            {
                SetValue((T?)value);
            }

            private void SetValue(T? value)
            {
                if (ValueToConfigStringFunc != null)
                {
                    var str = ValueToConfigStringFunc(value);
                    Config.Instance.SetValue(ConfigField, str);
                }
                else
                {
                    // TODO null ?
                    Config.Instance.SetValue(ConfigField, value?.ToString()??"");
                }
            }

            (string, string) IField.Validate()
            {
                if (ValidateFunc != null)
                {
                    return ValidateFunc(Value);
                }
                return ("", "");
            }
        }

        public List<IField> Fields { get; } = new List<IField>();

        public static Settings Instance { get; } = new Settings();

        public class FieldIndex
        {
            public const int DailyNotificationTime = 0;
            public const int ReminderDaysAhead = 1;

            public const int ConsultantName = 2;
            public const int OrganizationName = 3;
            public const int OrganizationPhone = 4;

            public const int MessageTemplate = 5;

            public const int ContactsAccountName = 6;
            public const int CalendarAccountName = 7;
            public const int CalendarDisplayName = 8;

            public const int EventTitleFormat = 9;      // TODO not used
            public const int AppAddedEventTitle = 10;   // TODO not used

            public const int Total = 11;
        }

        protected Settings()
        {
            Fields.AddRange(Enumerable.Repeat<IField>(default, FieldIndex.Total));

            Func<string, (TimeOnly, bool)> stringToTimeOnly = str =>
            {
                if (TimeOnly.TryParse(str, out var nt))
                {
                    return (nt, true);
                }
                return (default, false);
            };
            Fields[FieldIndex.DailyNotificationTime] = new Field<TimeOnly>
            {
                ConfigField = "daily_notification_time",
                EditorResourceId = Resource.Id.edit_notification_time,
                DefaultValue = new TimeOnly(17, 30),
                ValueToUIStringFunc = val => val.ToShortTimeString(),
                ValueToConfigStringFunc = val => val.ToShortTimeString(),
                ConfigStringToValueFunc = stringToTimeOnly,
                UIStringToValueFunc = stringToTimeOnly
            };

            Func<string, (int, bool)> stringToInt = str =>
            {
                if (int.TryParse(str, out var val))
                {
                    return (val, true);
                }
                return (default, false);
            };
            Fields[FieldIndex.ReminderDaysAhead] = new Field<int>
            {
                ConfigField = "reminder_days_ahead",
                EditorResourceId = Resource.Id.edit_reminder_day,
                DefaultValue = 1,
                ValueToUIStringFunc = val => val.ToString(),
                ValueToConfigStringFunc = val => val.ToString(),
                ConfigStringToValueFunc = stringToInt,
                UIStringToValueFunc = stringToInt
            };

            Fields[FieldIndex.ConsultantName] = new Field<string>
            {
                ConfigField = "consultant_name",
                EditorResourceId = Resource.Id.edit_practitioner_name
#if KMP
                , DefaultValue = "Cibo Cai"
#endif
            };

            Fields[FieldIndex.OrganizationName] = new Field<string>
            {
                ConfigField = "organization_name",
                EditorResourceId = Resource.Id.edit_organization_name
#if KMP
               , DefaultValue = "Kinetic Mobile Physio"
#endif
            };

            Fields[FieldIndex.OrganizationPhone] = new Field<string>
            {
                ConfigField = "organization_phone",
                EditorResourceId = Resource.Id.edit_organization_phone
#if KMP
                , DefaultValue = "0400693696"
#endif
            };

            Fields[FieldIndex.MessageTemplate] = new Field<string>
            {
                ConfigField = "message_template",
                EditorResourceId = Resource.Id.edit_message_template,
                DefaultValue = "Appointment reminder for <time> with <consultant> at <organization>. Please reply Y to confirm or call <phone> to reschedule. Thanks.",
                ValueToConfigStringFunc = val => val.ToString(),
                ValidateFunc = val =>
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        return ("", "Empty message template.");
                    }
                    var warnings = Utility.ValidateMessageTemplate(this);
                    return ("", warnings);
                }
            };

            Fields[FieldIndex.ContactsAccountName] = new Field<string>
            {
                ConfigField = "contacts_account_name",
                EditorResourceId = Resource.Id.edit_contacts_account_name,
                ValidateFunc = val =>
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        return ("", "Missing Contact account name.");
                    }
                    return ("", "");
                }
#if KMP
                , DefaultValue = "kineticmobilept@gmail.com"
#endif
            };

            Fields[FieldIndex.CalendarAccountName] = new Field<string>
            {
                ConfigField = "calendar_account_name",
                EditorResourceId = Resource.Id.edit_calendar_account_name,
                ValidateFunc = val =>
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        return ("", "Missing Calendar account name.");
                    }
                    return ("", "");
                }
#if KMP
                , DefaultValue = "kineticmobilept@gmail.com"
#endif
            };

            Fields[FieldIndex.CalendarDisplayName] = new Field<string>
            {
                ConfigField = "calendar_display_name",
                EditorResourceId = Resource.Id.edit_calendar_display_name,
                ValidateFunc = val =>
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        return ("", "Missing Calendar display name.");
                    }
                    return ("", "");
                }
#if KMP
                , DefaultValue = "kineticmobilept@gmail.com"
#endif
            };

            Fields[FieldIndex.EventTitleFormat] = new Field<string>
            {
                ConfigField = "event_title_format",
                EditorResourceId = Resource.Id.edit_event_title_format,
                ValidateFunc = val =>
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        return ("", "Missing event title format.");
                    }
                    return ("", "");
                }
#if KMP
                ,
                DefaultValue = "<client>( booking| [^A-Za-z '\\-].*|)"
#endif
            };


            Fields[FieldIndex.AppAddedEventTitle] = new Field<string>
            {
                ConfigField = "app_added_event_title",
                EditorResourceId = Resource.Id.edit_app_added_event_title,
                ValidateFunc = val =>
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        return ("", "Missing app added event title.");
                    }
                    return ("", "");
                },
#if KMP
                
                DefaultValue = "<client> booking"
#else
                DefaultValue = "[<client>]"
#endif
            };
        }
    }
}
