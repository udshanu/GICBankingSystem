using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GICBankingSystem.Test
{
    public class GICBankingTest
    {

        [Fact]
        public void CalculateBalanceToDate_ShouldCalculateCorrectBalance()
        {
            // Arrange
            DateTime targetDate = new DateTime(2023, 6, 1);
            BankingSystem bankingSystem = new BankingSystem();

            List<TransactionDTO> transactions = new List<TransactionDTO>
            {
                new TransactionDTO { Date = "20230505", Type = "D", Amount = 100 },
                new TransactionDTO { Date = "20230601", Type = "D", Amount = 150 },
                new TransactionDTO { Date = "20230626", Type = "W", Amount = 20 },
                new TransactionDTO { Date = "20230626", Type = "W", Amount = 100 }
            };

            // Act
            decimal balance = bankingSystem.CalculateBalanceToDate(targetDate, transactions);

            // Assert
            Assert.Equal(250, balance);
        }

        [Fact]
        public void FindApplicableInterestRule_ShouldReturnCorrectRule()
        {
            // Arrange
            DateTime targetDate = new DateTime(2023, 6, 1);
            BankingSystem bankingSystem = new BankingSystem();
            List<InterestRuleDTO> rules = new List<InterestRuleDTO>
            {
                new InterestRuleDTO { Date = "20230101", RuleId = "1", Rate = 1.95M },
                new InterestRuleDTO { Date = "20230520", RuleId = "2", Rate = 1.9M },
                new InterestRuleDTO { Date = "20230615", RuleId = "3", Rate = 2.2M }
            };

            // Act
            var applicableRule = bankingSystem.FindApplicableInterestRule(targetDate, rules);

            // Assert
            Assert.NotNull(applicableRule);
            Assert.Equal("2", applicableRule.RuleId);
        }

        [Fact]
        public void CalculateInterestWithAnnualization_ShouldReturnCorrectBalance()
        {
            // Arrange
            int targetMonth = 6;
            BankingSystem bankingSystem = new BankingSystem();

            List<InterestRuleDTO> rules = new List<InterestRuleDTO>
            {
                new InterestRuleDTO { Date = "20230101", RuleId = "1", Rate = 1.95M },
                new InterestRuleDTO { Date = "20230520", RuleId = "2", Rate = 1.9M },
                new InterestRuleDTO { Date = "20230615", RuleId = "3", Rate = 2.2M }
            };

            List<TransactionDTO> transactions = new List<TransactionDTO>
            {
                new TransactionDTO { Date = "20230505", Type = "D", Amount = 100 },
                new TransactionDTO { Date = "20230601", Type = "D", Amount = 150 },
                new TransactionDTO { Date = "20230626", Type = "W", Amount = 20 },
                new TransactionDTO { Date = "20230626", Type = "W", Amount = 100 }
            };

            bankingSystem.StartYear = 2023;
            bankingSystem.EndYear = 2023;

            decimal finalAnnualizedInterest = 0;

            // Act
            
            decimal calculatedBalance = bankingSystem.CalculateInterestWithAnnualization(targetMonth, rules, transactions, ref finalAnnualizedInterest);

            // Assert
            Assert.Equal(130.39M, Math.Ceiling(calculatedBalance * 100) / 100);
        }

        [Fact]
        public void CalculateBalance_ShouldReturnCorrectBalance()
        {
            // Arrange
            BankingSystem bankingSystem = new BankingSystem();
            List<TransactionDTO> transactions = new List<TransactionDTO>
            {
                new TransactionDTO { Date = "20230505", Type = "D", Amount = 100 },
                new TransactionDTO { Date = "20230601", Type = "D", Amount = 150 },
                new TransactionDTO { Date = "20230626", Type = "W", Amount = 20 },
                new TransactionDTO { Date = "20230626", Type = "W", Amount = 100 }
            };

            // Act
            decimal calculatedBalance = bankingSystem.CalculateBalance(transactions);

            // Assert
            Assert.Equal(130, calculatedBalance);
        }
    }
}
