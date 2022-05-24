using EllieMae.Encompass.Automation;
using System;
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
        private decimal _dotLineOneFeeAmount = 0m;
        private decimal _dotLineTwoFeeAmount = 0m;
        private decimal _dotLineThreeFeeAmount = 0m;
        private decimal _dotLineFourFeeAmount = 0m;
        private decimal _creditsEightZeroTwoAthroughDAmount = 0m;
        private decimal _bonafidePointsAmount = 0m;
        private decimal _originationDiscountPointsAmount = 0m;
        private decimal _bonafidePointsAmountAdjustment = 0m;

        private string _dotLineOneFeeName = "202";
        private string _dotLineOneFeeAmountField = "141";
        private string _dotLineTwoFeeName = "1091";
        private string _dotLineTwoFeeAmountField = "1095";
        private string _dotLineThreeFeeName = "1106";
        private string _dotLineThreeFeeAmountField = "1115";
        private string _dotLineFourFeeName = "1646";
        private string _dotLineFourFeeAmountField = "1647";
        private string _creditsEightZeroTwoAthroughD = "NEWHUD.X1149";
        private string _originationDiscountPoints = "NEWHUD.X1151";
        private string _bonafidePoints = "QM.X370";

        public void CalculateTxFeeCaps()
        {            
            PopulateFeeFields();
            PopulateApprovedFeeList();
            PolulateLoanFees();
            BonafidePoints();
            TotalFeeAmount();
            TotalCreditAmount();
            ValidateFeeTolerance();
        }

        private void BonafidePoints()
        {
            decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_originationDiscountPoints].UnformattedValue, out _originationDiscountPointsAmount);
            decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_bonafidePoints].UnformattedValue, out _bonafidePointsAmount);
            _bonafidePointsAmountAdjustment = _originationDiscountPointsAmount - _bonafidePointsAmount;
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
                    _totalFeesTXRefi += 165.00m;
                }
                else
                {
                    _totalFeesTXRefi += item.FeeAmount;
                }                
            }
            _totalFeesTXRefi += _bonafidePointsAmountAdjustment;
        }

        private void TotalCreditAmount()
        {
            if (EncompassApplication.CurrentLoan.Fields[_dotLineOneFeeName].FormattedValue == "Other Credit")
            {
                decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_dotLineOneFeeAmountField].FormattedValue, out _dotLineOneFeeAmount);
            }
            if (EncompassApplication.CurrentLoan.Fields[_dotLineTwoFeeName].FormattedValue == "Other Credit")
            {
                decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_dotLineTwoFeeAmountField].FormattedValue, out _dotLineTwoFeeAmount);
            }
            if (EncompassApplication.CurrentLoan.Fields[_dotLineThreeFeeName].FormattedValue == "Other Credit")
            {
                decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_dotLineThreeFeeAmountField].FormattedValue, out _dotLineThreeFeeAmount);
            }
            if (EncompassApplication.CurrentLoan.Fields[_dotLineFourFeeName].FormattedValue == "Other Credit")
            {
                decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_dotLineFourFeeAmountField].FormattedValue, out _dotLineFourFeeAmount);                
            }
            decimal.TryParse(EncompassApplication.CurrentLoan.Fields[_creditsEightZeroTwoAthroughD].FormattedValue, out _creditsEightZeroTwoAthroughDAmount);
            if ((_dotLineOneFeeAmount + _dotLineTwoFeeAmount + _dotLineThreeFeeAmount + _dotLineFourFeeAmount) > 0)
            {
                HealthCheck.HealthCheckMessage += "Other Credits";
            }
            _lenderCredit = _dotLineOneFeeAmount + _dotLineTwoFeeAmount + _dotLineThreeFeeAmount + _dotLineFourFeeAmount + _creditsEightZeroTwoAthroughDAmount;
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
