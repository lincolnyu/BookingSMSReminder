using Android.Content;

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
    }
}
