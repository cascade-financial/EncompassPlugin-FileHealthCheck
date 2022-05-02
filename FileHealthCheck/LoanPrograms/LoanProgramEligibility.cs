using EllieMae.Encompass.Automation;
using System;
using System.Windows.Forms;

namespace FileHealthCheck
{
    public class LoanProgramEligibility
    {        
        public LoanProgramEligibility()
        {
        }

        public bool RunHealthChecks()
        {
            bool programEligibility = GetLoanProgramEligibility(HealthCheck.LoanProgram);              
            return true;
        }

        public bool GetLoanProgramEligibility(string loanProgram)
        {
            Fees.FeeLogic feeLogic = new Fees.FeeLogic();
            string message = "";
            try
            {
                switch (loanProgram)
                {
                    case "conv refi":
                        if (HealthCheck.SubjectPropertyState == "tx" && HealthCheck.LoanPurpose == "cash-out refinance")
                        {
                            feeLogic.CalculateTxFeeCaps();
                            if (HealthCheck.RevisedCdSentDate != null && HealthCheck.RevisedCdSentDate.ToString() != "1/1/0001 12:00:00 AM")
                            {
                                ValidateClosingDate();
                            }                           
                            switch (HealthCheck.SubjectPropertyType)
                            {
                                case "attached":

                                case "detached":

                                case "modular":

                                case "pud":

                                case "condominium":

                                case "manufacturedhousing":
                                    break;
                                default:
                                    message += $"Subject property type [1041] '{HealthCheck.SubjectPropertyType}' is not allowed for {HealthCheck.LoanProgram} \n";
                                    break;
                            }
                            if (HealthCheck.SubjectPropertyWidth == "singlewide")
                            {
                                message += $"Singlewides are not allowed for {HealthCheck.LoanProgram} \n";
                            }
                            if (HealthCheck.SubjectPropertyType == "manufacturedhousing")
                            {
                                if (HealthCheck.LoanLtv > 65m)
                                {
                                    message += $"LTV [353] of '{HealthCheck.LoanLtv}' with a {HealthCheck.SubjectPropertyType} is too high for {HealthCheck.LoanProgram} \n";
                                }
                                if (HealthCheck.LoanTerm > 240)
                                {
                                    message += $"Loan term [4] of '{HealthCheck.LoanTerm}' for a [1041] {HealthCheck.SubjectPropertyType} exceeds the maximum 240 term for {HealthCheck.LoanProgram} \n";
                                }
                            }
                            else
                            {
                                if (HealthCheck.LoanLtv > 80m)
                                {
                                    message += $"LTV [353] of '{HealthCheck.LoanLtv}' with a [1041] {HealthCheck.SubjectPropertyType} is too high for {HealthCheck.LoanProgram} \n";
                                }
                                if (HealthCheck.LoanTerm > 360)
                                {
                                    message += $"Loan term [4] of '{HealthCheck.LoanTerm}' for a [1041] {HealthCheck.SubjectPropertyType} exceeds the maximum 360 term for {HealthCheck.LoanProgram} \n";
                                }
                            }
                            if (HealthCheck.LoanFinancing == "secondlien")
                            {
                                message += $"Subordinate Financing [420] of '{HealthCheck.LoanFinancing}' is not allowed for {HealthCheck.LoanProgram} \n";
                            }
                            switch (HealthCheck.SubjectPropertyOccupancy)
                            {
                                case "secondhome":
                                    if (HealthCheck.LoanTexasA4 != "y")
                                    {
                                        message += $"Occupancy Type [1811] of '{HealthCheck.SubjectPropertyOccupancy}' is not allowed for {HealthCheck.LoanProgram} \n";
                                    }                                    
                                    break;
                                case "investor":
                                    if (HealthCheck.LoanTexasA4 != "y")
                                    {
                                        message += $"Occupancy Type [1811] of '{HealthCheck.SubjectPropertyOccupancy}' is not allowed for {HealthCheck.LoanProgram} \n";
                                    }
                                    break;
                                default:
                                    break;
                            }
                            if (HealthCheck.LoanType != "conventional")
                            {
                                message += $"Loan type [1172] of '{HealthCheck.LoanType}' is not allowed for {HealthCheck.LoanProgram} \n";
                            }
                            if (message.Length > 0)
                            {                               
                                HealthCheck.HealthCheckMessage += "Invalid Texas A6";                                
                            }
                        }
                        else if (HealthCheck.SubjectPropertyState == "tx" && HealthCheck.LoanPurpose == "nocash-out refinance")
                        {
                            message += "y" == HealthCheck.LoanTexasA4 ? "" : $"Texax A4 [Disclosure.X1174] of '{HealthCheck.LoanTexasA4}' is not allowed for {HealthCheck.LoanProgram} \n";
                            NoPrincipalReductions();
                            NoCashToBorrower();
                            if (message.Length > 0)
                            {
                                HealthCheck.HealthCheckMessage += "Invalid Texas A4";
                            }
                        }                 
                        break;
                    default:                        
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error Validating Loan Program Eligibility");
                return false;
            }
        }       

        private void NoPrincipalReductions()
        {
            try
            {
                bool isPrincipalReduction = false;
                for (int i = 50; i < 64; i++)
                {
                    if (EncompassApplication.CurrentLoan.Fields["CD3.X" + i.ToString()].FormattedValue == "Principal Reduction to Borrower")
                    {
                        isPrincipalReduction = true;
                    }
                }
                if (isPrincipalReduction)
                {
                    HealthCheck.HealthCheckMessage += "Principal Reduction on TX (a)(4)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error Validating Principale Reduction");
            }
        }

        private void NoCashToBorrower()
        {
            if (HealthCheck.TotalCashToCloseTo == "to borrower" && HealthCheck.TotalCashToClose > 0)
            {
                HealthCheck.HealthCheckMessage += "Need Cash to Close on TX (a)(4)";
            }
        }

        private void ValidateClosingDate()
        {
            if (HealthCheck.RegZBusinessCalendar.AddBusinessDays(HealthCheck.RevisedCdSentDate,1,true) >= HealthCheck.ClosingDate)
            {
                HealthCheck.HealthCheckMessage += "TX (a)(6) Invalid Closing Date";
            }
        }
    }
}
