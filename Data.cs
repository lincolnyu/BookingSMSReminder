using Android.Content;
using Android.Database;
using Android.Provider;

namespace BookingSMSReminder
{
    public class Data
    {
        public class Contact
        {
            public Contact(string displayName) { DisplayName = displayName; }
            public string DisplayName;
            public string? MostLikelyNumber;
        }

        private static Data? instance_ = null;

        public static Data Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = new Data();
                }
                return instance_;
            }
        }

        private Data() { }

        public Dictionary<string, Contact> Contacts { get; private set; } = new Dictionary<string, Contact>();

        public void ReloadContacts(Context context)
        {
            Contacts = LoadContacts(context).ToDictionary(x => x.DisplayName.ToLower());
        }

        private IEnumerable<Contact> LoadContacts(Context context)
        {
            var uri = ContactsContract.RawContacts.ContentUri;
            string[] projection = {
                ContactsContract.RawContacts.InterfaceConsts.ContactId,
                ContactsContract.RawContacts.InterfaceConsts.DisplayNamePrimary,
            };

            var selectionString = ContactsContract.RawContacts.InterfaceConsts.AccountName + "=?";
            var selectionStringArgs = new string[] { "kineticmobilept@gmail.com" };
            var loader = new CursorLoader(context, uri, projection, selectionString, selectionStringArgs, null);
            var cursor = (ICursor)loader.LoadInBackground();

            if (cursor != null && cursor.MoveToFirst())
            {
                do
                {
                    var id = cursor.GetString(cursor.GetColumnIndex(projection[0]));
                    //Phone Numbers
                    string[] columnsNames2 = [
                        ContactsContract.CommonDataKinds.Phone.Number,
                    ];

                    //Store Contact ID
                    string[] selectionStringArgs2 = [id];
                    string selectionString2 = ContactsContract.CommonDataKinds.Phone.InterfaceConsts.ContactId + "=?";
                    var loader2 = new CursorLoader(context, ContactsContract.CommonDataKinds.Phone.ContentUri, columnsNames2, selectionString2, selectionStringArgs2, null);
                    var cursor2 = (ICursor)loader2.LoadInBackground();

                    string? mostLikelyNumber = null;
                    if (cursor2 != null && cursor2.MoveToFirst())
                    {
                        do
                        {
                            var number = cursor2.GetString(cursor2.GetColumnIndex(columnsNames2[0])).Trim().Replace(" ", "");
                            if (number.StartsWith("04") || number.StartsWith("+614"))
                            {
                                // normalize to 04
                                if (number.StartsWith("+614"))
                                {
                                    number = "0" + number[3..];
                                }
                                mostLikelyNumber = number;
                                break;
                            }
                        }
                        while (cursor2.MoveToNext());
                    }

                    yield return new Contact(displayName: cursor.GetString(cursor.GetColumnIndex(projection[1])))
                    {
                        MostLikelyNumber = mostLikelyNumber
                    };
                } while (cursor.MoveToNext());
            }
        }
    }
}
