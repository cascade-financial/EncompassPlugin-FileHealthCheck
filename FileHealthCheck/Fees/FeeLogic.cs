using EllieMae.Encompass.Automation;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileHealthCheck.Fees
{
    public class FeeLogic
    {
        private List<FeeFields> _feeFieldsTXRefi = new List<FeeFields>();
        private List<LoanFees> _loanFeesTXRefi = new List<LoanFees>();
        private List<ApprovedFees> _approvedFeesTXRefi = new List<ApprovedFees>();

        private decimal _lenderCredit = 0m;
        private decimal _totalFeesTXRefi = 0m;
        private decimal _feeCapPercent = 0m;
        private decimal _DotLineOneFeeAmount = 0m;
        private decimal _DotLineTwoFeeAmount = 0m;
        private decimal _DotLineThreeFeeAmount = 0m;
        private decimal _DotLineFourFeeAmount = 0m;
        private decimal _CreditsEightZeroTwoAthroughDAmount = 0m;

        private string _DotLineOneFeeName = "202";
        private string _DotLineOneFeeAmountField = "141";
        private string _DotLineTwoFeeName = "1091";
        private string _DotLineTwoFeeAmountField = "1095";
        private string _DotLineThreeFeeName = "1106";
        private string _DotLineThreeFeeAmountField = "1115";
        private string _DotLineFourFeeName = "1646";
        private string _DotLineFourFeeAmountField = "1647";
        private string _CreditsEightZeroTwoAthroughD = "NEWHUD.X1149";

        public void CalculateTxFeeCaps()
        {            
            PopulateFeeFields();
            PopulateApprovedFeeList();
            PolulateLoanFees();
            TotalFeeAmount();
            TotalCreditAmount();
            ValidateFeeTolerance();
        }

        private void PopulateFeeFields()
        {
            EllieMae.Encompass.BusinessObjects.DataObject feeFields = EncompassApplication.Session.DataExchange.GetCustomDataObject("ItemizationFeeFields.csv");
            StreamReader sr = new StreamReader(feeFields.OpenStream());
            while (!sr.EndOfStream)
            {
                string[] rows = sr.ReadLine().Split(',');
                if (rows[0].ToString().ToLower() == "x")
                {
                    _feeFieldsTXRefi.Add(new FeeFields { HardCodedFeeName = rows[1], FeeAmountField = rows[2], FeeCreditField = rows[3] });
                }
                else
                {
                    _feeFieldsTXRefi.Add(new FeeFields { FeeNameField = rows[1], FeeAmountField = rows[2], FeeCreditField = rows[3] });
                }
            }
        }

        private void PopulateApprovedFeeList()
        {
            EllieMae.Encompass.BusinessObjects.DataObject approvedFees = EncompassApplication.Session.DataExchange.GetCustomDataObject("TXApprovedFees.csv");
            StreamReader sr = new StreamReader(approvedFees.OpenStream());
            while (!sr.EndOfStream)
            {
                string[] rows = sr.ReadLine().Split(',');
                _approvedFeesTXRefi.Add(new ApprovedFees { FeeName = rows[0], Priority = int.Parse(rows[1]) });
            }
        }

        private void PolulateLoanFees()
        {
            ApprovedFees approvedFee;
            string feeName = "";
            decimal feeAmount = 0m;
            foreach (FeeFields item in _feeFieldsTXRefi)
            {
                if (!string.IsNullOrWhiteSpace(item.HardCodedFeeName))
                {
                    feeName = item.HardCodedFeeName;
                }
                else
                {
                    feeName = EncompassApplication.CurrentLoan.Fields[item.FeeNameField].FormattedValue;
                }
                approvedFee = _approvedFeesTXRefi.FirstOrDefault(x => x.FeeName == feeName);
                if (approvedFee != null)
                {
                    decimal.TryParse(EncompassApplication.CurrentLoan.Fields[item.FeeAmountField].UnformattedValue, out feeAmount);
                    _loanFeesTXRefi.Add(new LoanFees
                    {
                        FeeName = approvedFee.FeeName,
                        Priority = approvedFee.Priority,
                        FeeNameField = item.FeeNameField,
                        FeeAmountField = item.FeeAmountField,
                        FeeCreditField = item.FeeCreditField,
                        FeeAmount = feeAmount
                    });
                }
            }
            _loanFeesTXRefi = _loanFeesTXRefi.OrderBy(o => o.Priority).ToList();
        }

        private void TotalFeeAmount()
        {
            foreach (LoanFees item in _loanFeesTXRefi)
            {
                if (item.FeeName == "Appraisal Fee" && item.FeeAmount > 165.00m)
                {
                    _totalFeesTXRefi = _totalFeesTXRefi + 165.00m;
                }
                else
                {
                    _totalFeesTXRefi = _totalFeesTXRefi + item.FeeAmount;
                }                
            }
        }

        private void TotalCreditAmount()
        {
            if (EncompassApplication.CurrentLoan.Fields[_DotLineOneFeeName].FormattedValue == "Other Credit")
            {
                decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_DotLineOneFeeAmountField].FormattedValue, out _DotLineOneFeeAmount);
            }
            if (EncompassApplication.CurrentLoan.Fields[_DotLineTwoFeeName].FormattedValue == "Other Credit")
            {
                decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_DotLineTwoFeeAmountField].FormattedValue, out _DotLineTwoFeeAmount);
            }
            if (EncompassApplication.CurrentLoan.Fields[_DotLineThreeFeeName].FormattedValue == "Other Credit")
            {
                decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_DotLineThreeFeeAmountField].FormattedValue, out _DotLineThreeFeeAmount);
            }
            if (EncompassApplication.CurrentLoan.Fields[_DotLineFourFeeName].FormattedValue == "Other Credit")
            {
                decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_DotLineFourFeeAmountField].FormattedValue, out _DotLineFourFeeAmount);                
            }
            decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_CreditsEightZeroTwoAthroughD].FormattedValue, out _CreditsEightZeroTwoAthroughDAmount);
            if ((_DotLineOneFeeAmount + _DotLineTwoFeeAmount + _DotLineThreeFeeAmount + _DotLineFourFeeAmount) > 0)
            {
                HealthCheck.HealthCheckMessage += "Other Credits";
            }
            _lenderCredit = _DotLineOneFeeAmount + _DotLineTwoFeeAmount + _DotLineThreeFeeAmount + _DotLineFourFeeAmount + _CreditsEightZeroTwoAthroughDAmount;
        }

        private void ValidateFeeTolerance()
        {
            if (HealthCheck.SubjectPropertyState == "tx" && HealthCheck.LoanPurpose == "cash-out refinance")
            {
                _feeCapPercent = .02m;
                HealthCheck.ExcessFeeAmount = (_totalFeesTXRefi - _lenderCredit) - (HealthCheck.LoanAmount * _feeCapPercent);
                if (HealthCheck.LoanAmount * _feeCapPercent < _totalFeesTXRefi - _lenderCredit)
                {
                    HealthCheck.HealthCheckMessage += "Exceeds TX Fee Cap";
                }
            }
        }
    }
}
