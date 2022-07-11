using EllieMae.Encompass.Automation;
using EllieMae.Encompass.BusinessObjects.Loans;
using EllieMae.Encompass.ComponentModel;
using EllieMae.Encompass.Configuration;
using FileHealthCheck.Alerts;
using System;

namespace FileHealthCheck
{
    [Plugin]
    public class HealthCheck
    {
        public static string HealthCheckMessage { get; set; }
        private string _healhCheckAlertField = "CX.HEALTH.CHECK.ALERTS";
        public static decimal ExcessFeeAmount { get; set; }
        private string _excessFeeAmount = "CX.HEALTH.CHECK.EXCESS.FEES";
        public static string LoanProgram { get; set; }
        public static string SubjectPropertyState { get; set; }
        public static string SubjectPropertyType { get; set; }
        public static string SubjectPropertyWidth { get; set; }
        public static string SubjectPropertyOccupancy { get; set; }
        public static string LoanPurpose { get; set; }
        public static string LoanType { get; set; }
        public static string LoanFinancing { get; set; }
        public static string LoanAmortizationType { get; set; }
        public static string LoanTexasA4 { get; set; }
        public static int SubjectPropertyUnits { get; set; }
        public static decimal LoanLtv { get; set; }
        public static decimal LoanAmount { get; set; }
        public static decimal TotalCashToClose { get; set; }
        public static string TotalCashToCloseTo{ get; set; }
        public static string LeDetailsSnapshot { get; set; }
        public static int LoanTerm { get; set; }
        public static DateTime ClosingDate { get; set; }
        public static DateTime RevisedCdReceivedDate { get; set; }
        public static DateTime InitialLeSentDate { get; set; }
        public static BusinessCalendar RegZBusinessCalendar { get; set; }

        private int _subjectPropertyUnits = 0;
        private decimal _loanLtv = 0m;
        private decimal _loanAmount = 0m;
        private decimal _cashToClose = 0m;
        private int _loanTerm = 0;
        private DateTime _closingDate;
        private DateTime _revisedCdReceivedDate;
        private DateTime _initalLeSentDate;

        public HealthCheck()
        {
            EncompassApplication.LoanOpened += new EventHandler(LoanOpened);
        }

        private void LoanOpened(object sender, EventArgs e)
        {
            EncompassApplication.CurrentLoan.BeforeCommit += new CancelableEventHandler(CurrentLoan_BeforeCommit);
        }

        private void SetProperties()
        {
            int.TryParse(EncompassApplication.CurrentLoan.Fields["16"].FormattedValue, out _subjectPropertyUnits);
            decimal.TryParse(EncompassApplication.CurrentLoan.Fields["353"].FormattedValue, out _loanLtv);
            decimal.TryParse(EncompassApplication.CurrentLoan.Fields["1109"].FormattedValue, out _loanAmount);
            decimal.TryParse(EncompassApplication.CurrentLoan.Fields["CD1.X69"].FormattedValue, out _cashToClose);
            int.TryParse(EncompassApplication.CurrentLoan.Fields["4"].FormattedValue, out _loanTerm);
            DateTime.TryParse(EncompassApplication.CurrentLoan.Fields["748"].FormattedValue, out _closingDate);
            DateTime.TryParse(EncompassApplication.CurrentLoan.Fields["3980"].FormattedValue, out _revisedCdReceivedDate);
            DateTime.TryParse(EncompassApplication.CurrentLoan.Fields["3152"].FormattedValue, out _initalLeSentDate);
            LeDetailsSnapshot = EncompassApplication.CurrentLoan.Fields["CX.LE.LOAN.DETAILS.SNAPSHOT"].FormattedValue.ToLower();
            LoanProgram = EncompassApplication.CurrentLoan.Fields["1401"].FormattedValue.ToLower();
            SubjectPropertyState = EncompassApplication.CurrentLoan.Fields["14"].FormattedValue.ToLower();
            SubjectPropertyType = EncompassApplication.CurrentLoan.Fields["1041"].FormattedValue.ToLower();
            SubjectPropertyOccupancy = EncompassApplication.CurrentLoan.Fields["1811"].FormattedValue.ToLower();
            LoanPurpose = EncompassApplication.CurrentLoan.Fields["19"].FormattedValue.ToLower();
            LoanType = EncompassApplication.CurrentLoan.Fields["1172"].FormattedValue.ToLower();
            LoanFinancing = EncompassApplication.CurrentLoan.Fields["420"].FormattedValue.ToLower();
            LoanAmortizationType = EncompassApplication.CurrentLoan.Fields["608"].FormattedValue.ToLower();
            LoanTexasA4 = EncompassApplication.CurrentLoan.Fields["DISCLOSURE.X1174"].FormattedValue.ToLower();
            TotalCashToCloseTo = EncompassApplication.CurrentLoan.Fields["CD3.X86"].FormattedValue.ToLower();
            SubjectPropertyWidth = EncompassApplication.CurrentLoan.Fields["CX.GLOBAL.WIDTHSELECT"].FormattedValue.ToLower();
            RegZBusinessCalendar = EncompassApplication.Session.SystemSettings.GetBusinessCalendar(BusinessCalendarType.RegZ);
            SubjectPropertyUnits = _subjectPropertyUnits;
            LoanLtv = _loanLtv;
            LoanAmount = _loanAmount;
            TotalCashToClose = _cashToClose;
            LoanTerm = _loanTerm;
            ClosingDate = _closingDate;
            RevisedCdReceivedDate = _revisedCdReceivedDate;
            InitialLeSentDate = _initalLeSentDate;
        }

        public void CurrentLoan_BeforeCommit(object source, CancelableEventArgs e)
        {
            SetProperties();
            RunHealthChecks();
            UpdateEncompassFields();
        }

        public void StartRunningHealthChecks()
        {
            SetProperties();
            RunHealthChecks();
            UpdateEncompassFields();
        }

        private void UpdateEncompassFields()
        {
            EncompassApplication.CurrentLoan.Fields[_healhCheckAlertField].Value = HealthCheckMessage;
            EncompassApplication.CurrentLoan.Fields[_excessFeeAmount].Value = ExcessFeeAmount;
            HealthCheckMessage = "";
        }

        private void RunHealthChecks()
        {
            HealthCheckMessage = "";
            bool LoanProgramCheckResult = LoanProgramCheck();
            bool LoanAlertsCheckResult = LoanAlertsCheck();
        }

        private bool LoanAlertsCheck()
        {
            LoanAlerts loanAlerts = new LoanAlerts();
            return loanAlerts.CheckForAlerts();
        }          

        public bool LoanProgramCheck()
        {
            LoanProgramEligibility loanProgramEligibility = new LoanProgramEligibility();
            bool result = loanProgramEligibility.GetLoanProgramEligibility(HealthCheck.LoanProgram);
            return result;
        }        
    }
}
