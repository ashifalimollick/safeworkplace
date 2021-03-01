using System.Collections.Generic;
using System.Linq;

namespace FinanceBot
{
    /// <summary>
    /// Class to store conversation data. We need a dictionary structure to pass the conversation state to dialogs.
    /// </summary>
    public class UserData : Dictionary<string, object>
    {
        private const string FacilityTypeKey = "FacilityType";
        private const string EmployeeIDKey = "EmployeeID";
        private const string FacilityIDKey = "FacilityID";
        private const string FacilityPreferenceKey = "FacilityPreference";
        private const string PersonAmountKey = "PersonAmount";
        private const string FloorKey = "Floor";
        private const string DateKey = "Date";
        private const string TimeKey = "Time";
        private const string PreviousQnAIDKey = "PreviousQnAID";
        private const string PreviousQnAQuestionAskedKey = "PreviousQnAQuestionAsked";
        private const string QuestionKey = "Question";
        private const string LogKey = "Log";

        public UserData()
        {
            this[PreviousQnAIDKey] = null;
            this[PreviousQnAQuestionAskedKey] = null;
            this[FacilityTypeKey] = null;
            this[EmployeeIDKey] = null;
            this[FacilityIDKey] = null;
            this[FacilityPreferenceKey] = null;
            this[PersonAmountKey] = null;
            this[FloorKey] = null;
            this[DateKey] = null;
            this[TimeKey] = null;
            this[QuestionKey] = null;
            this[LogKey] = null;
        }

        public UserData(IDictionary<string, object> source)
        {
            if (source != null)
            {
                source.ToList().ForEach(x => this.Add(x.Key, x.Value));
            }
        }

        public string PreviousQnAID
        {
            get { return (string)this[PreviousQnAIDKey]; }
            set { this[PreviousQnAIDKey] = value; }
        }

        public string PreviousQnAQuestionAsked
        {
            get { return (string)this[PreviousQnAQuestionAskedKey]; }
            set { this[PreviousQnAQuestionAskedKey] = value; }
        }

        public string FacilityType
        {
            get { return (string)this[FacilityTypeKey]; }
            set { this[FacilityTypeKey] = value; }
        }

        public string Question
        {
            get { return (string)this[QuestionKey]; }
            set { this[QuestionKey] = value; }
        }

        public string Log
        {
            get { return (string)this[LogKey]; }
            set { this[LogKey] = value; }
        }


        public string EmployeeID
        {
            get { return (string)this[EmployeeIDKey]; }
            set { this[EmployeeIDKey] = value; }
        }

        public string FacilityID
        {
            get { return (string)this[FacilityIDKey]; }
            set { this[FacilityIDKey] = value; }
        }

        public string FacilityPreference
        {
            get { return (string)this[FacilityPreferenceKey]; }
            set { this[FacilityPreferenceKey] = value; }
        }

        public string PersonAmount
        {
            get { return (string)this[PersonAmountKey]; }
            set { this[PersonAmountKey] = value; }
        }

        public string Floor
        {
            get { return (string)this[FloorKey]; }
            set { this[FloorKey] = value; }
        }

        public string Date
        {
            get { return (string)this[DateKey]; }
            set { this[DateKey] = value; }
        }

        public string Time
        {
            get { return (string)this[TimeKey]; }
            set { this[TimeKey] = value; }
        }
    }

}
