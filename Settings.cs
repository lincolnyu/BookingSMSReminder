namespace BookingSMSReminder
{
    public class Settings
    {
        public interface IField
        {
            string ConfigField { get; }
            int? EditorResourceId { get; }

            void UpdateConfigToUI(Activity activity);

            /// <summary>
            ///  Updates the field value to config with validation
            /// </summary>
            /// <param name="value">The value to convert</param>
            /// <returns>Error string if there's an error.</returns>
            void UpdateToConfig(object? value);

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

            public Func<T, string>? ValueToUIString;
            public Func<T, string>? ValueToConfigString;

            /// <summary>
            ///  Return non-empty strings for validation errors and warnings respectively.
            /// </summary>
            public Func<(string, string)>? Validate;

            // Returns null upon parse failure
            public Func<string, (T, bool)>? UIStringToValue;
            public Func<string, (T, bool)>? ConfigStringToValue;

            /// <summary>
            ///  Load the value from the config or fall back on the default
            /// </summary>
            public T? Value
            {
                get
                {
                    var str = Config.Instance.GetValue(ConfigField);
                    if (ConfigStringToValue != null && str != null)
                    {
                        var (v, succ) = ConfigStringToValue(str);
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
            }

            /// <summary>
            ///  Convert UI string to the represented value using UIStringToValue. If UIStringToValue is unavailable, the UI string is retured as is.
            /// </summary>
            /// <param name="str">The string corresponding to the value in the UI</param>
            /// <returns>The value and whether it exits</returns>
            public (object?, bool) ConvertUIStringToValue(string str)
            {
                if (UIStringToValue != null)
                {
                    var (val, succ) = UIStringToValue(str);
                    return (val, succ);
                }
                else
                {
                    return (str, true);
                }
            }

            public void UpdateConfigToUI(Activity activity)
            {
                if (EditorResourceId != null)
                {
                    var editText = activity.FindViewById<EditText>(EditorResourceId.Value);
                    var val = Value;
                    string uistr;
                    if (ValueToUIString != null)
                    {
                        uistr = ValueToUIString(val);
                    }
                    else
                    {
                        uistr = val?.ToString()?? "";
                    }
                    editText.Text = uistr;
                }
            }

            void IField.UpdateToConfig(object? value)
            {
                UpdateToConfig((T?)value);
            }

            public void UpdateToConfig(T? value)
            {
                if (ValueToConfigString != null)
                {
                    var str = ValueToConfigString(value);
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
                if (Validate != null)
                {
                    return Validate();
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

            public const int Total = 9;
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
                ValueToUIString = val => val.ToShortTimeString(),
                ValueToConfigString = val => val.ToShortTimeString(),
                ConfigStringToValue = stringToTimeOnly,
                UIStringToValue = stringToTimeOnly
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
                ValueToUIString = val => val.ToString(),
                ValueToConfigString = val => val.ToString(),
                ConfigStringToValue = stringToInt,
                UIStringToValue = stringToInt
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
                ValueToConfigString = val => val.ToString(),
                Validate = ()=>
                {
                    var len = Utility.EvaluateMessageLength(this);
                    if (len > 160)
                    {
                        return ("", "The message may exceeding the length limit of 160");
                    }
                    return ("", "");
                }
            };

            Fields[FieldIndex.ContactsAccountName] = new Field<string>
            {
                ConfigField = "contacts_account_name",
                EditorResourceId = Resource.Id.edit_contacts_account_name
#if KMP
                , DefaultValue = "kineticmobilept@gmail.com"
#endif
            };

            Fields[FieldIndex.CalendarAccountName] = new Field<string>
            {
                ConfigField= "calendar_account_name",
                EditorResourceId  = Resource.Id.edit_calendar_account_name
#if KMP
                , DefaultValue = "kineticmobilept@gmail.com"
#endif
            };

            Fields[FieldIndex.CalendarDisplayName] = new Field<string>
            {
                ConfigField = "calendar_display_name",
                EditorResourceId = Resource.Id.edit_calendar_display_name
#if KMP
                , DefaultValue = "kineticmobilept@gmail.com"
#endif
            };
        }
    }
}
