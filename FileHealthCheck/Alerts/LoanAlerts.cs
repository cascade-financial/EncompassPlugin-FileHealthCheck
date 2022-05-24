using System;
using System.Windows.Forms;

namespace FileHealthCheck.Alerts
{
    public class LoanAlerts
    {
        public bool CheckForAlerts()
        {
            try
            {
                if (HealthCheck.SubjectPropertyState == "tx" && HealthCheck.LoanProgram == "conv refi")
                {
                    if (HealthCheck.InitialLeSentDate != null && HealthCheck.InitialLeSentDate.ToString() != "1/1/0001 12:00:00 AM")
                    {
                        if (HealthCheck.LoanPurpose == "nocash-out refinance" && 
                            (string.IsNullOrEmpty(HealthCheck.LeDetailsSnapshot) || !HealthCheck.LeDetailsSnapshot.Contains("loan purpose: nocash-out refinance")) && 
                            HealthCheck.LoanTexasA4 == "y")
                        {
                            HealthCheck.HealthCheckMessage += "Confirm A4 Disclosure Sent";
                        }
                        else if (HealthCheck.LoanPurpose == "cash-out refinance" && (string.IsNullOrEmpty(HealthCheck.LeDetailsSnapshot) || !HealthCheck.LeDetailsSnapshot.Contains("loan purpose: cash-out refinance")))
                        {
                            HealthCheck.HealthCheckMessage += "Confirm A6 Disclosure Sent";
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error Validating Alerts");
                return false;
            }
        }
    }
}
