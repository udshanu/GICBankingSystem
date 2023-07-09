using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Transactions;

namespace GICBankingSystem
{
    
    internal class Program
    {
        static void Main(string[] args)
        {
            bool isFirstTimeUser = true;
            BankingSystem bankingSystem = new BankingSystem();
            bankingSystem.Run(isFirstTimeUser);
        }
    }

    public class TransactionDTO
    {
        public string Date { get; set; }
        public string Account { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }
    }

    public class InterestRuleDTO
    {
        public string Date { get; set; }
        public string RuleId { get; set; }
        public decimal Rate { get; set; }
    }

    public class BankAccountDTO
    {
        public string AccountNumber { get; set; }
        public List<TransactionDTO> Transactions { get; set; } = new List<TransactionDTO>();
        public List<InterestRuleDTO> InterestRules { get; set; } = new List<InterestRuleDTO>();
    }

    public class BankingSystem
    {
        public int StartYear { get; set; }
        public int EndYear { get; set; }

        public List<BankAccountDTO> BankAccounts = new List<BankAccountDTO>();
        public List<InterestRuleDTO> InterestRules = new List<InterestRuleDTO>();

        public void Run(bool isFirstTimeUser)
        {
            while (true)
            {
                // Display appropriate message based on isFirstTimeUser flag
                if (isFirstTimeUser)
                {
                    Console.WriteLine("Welcome to AwesomeGIC Bank! What would you like to do?");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Is there anything else you'd like to do?");
                }

                // Display available options to the user
                Console.WriteLine("[I]nput transactions");
                Console.WriteLine("[D]efine interest rules");
                Console.WriteLine("[P]rint statement");
                Console.WriteLine("[Q]uit");
                Console.Write("> ");

                string input = Console.ReadLine().Trim();

                // Process user input using a switch statement
                switch (input.ToUpper())
                {
                    case "I":
                        InputTransactions();
                        break;
                    case "D":
                        DefineInterestRules();
                        break;
                    case "P":
                        PrintStatement();
                        break;
                    case "Q":
                        Quit();
                        return;
                    default:
                        Console.WriteLine();
                        Console.WriteLine("Invalid input. Please try again.");
                        break;
                }

                // Update isFirstTimeUser flag after the first iteration
                isFirstTimeUser = false;
            }
        }

        public void InputTransactions()
        {
            // Prompt the user to enter transaction details
            Console.WriteLine();
            Console.WriteLine("Please enter transaction details in <Date>|<Account>|<Type>|<Amount> format");
            Console.WriteLine("(or enter blank to go back to the main menu):");
            Console.Write("> ");

            string input = Console.ReadLine().Trim();

            // If input is blank, return to the main menu
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            string[] parts = input.Split('|');

            // Check if input has the correct number of parts
            if (parts.Length != 4)
            {
                Console.WriteLine();
                Console.WriteLine("Invalid input format. Please try again.");
                return;
            }

            string date = parts[0];
            string account = parts[1];
            string type = parts[2];
            decimal amount;

            // Validate the date format
            if (!IsValidDateFormat(date))
            {
                Console.WriteLine();
                Console.WriteLine("Invalid date format. Please enter a valid date.");
                return;
            }

            if (!decimal.TryParse(parts[3], out amount))
            {
                Console.WriteLine();
                Console.WriteLine("Invalid amount. Please enter a valid decimal number.");
                return;
            }

            // Check if the amount is greater than zero
            if (amount <= 0)
            {
                Console.WriteLine();
                Console.WriteLine("Amount must be greater than zero.");
                return;
            }

            // Find the bank account associated with the provided account number
            BankAccountDTO bankAccount = BankAccounts.Find(acc => acc.AccountNumber == account);

            // If the bank account does not exist, create a new one
            if (bankAccount == null)
            {
                bankAccount = new BankAccountDTO { AccountNumber = account };
                BankAccounts.Add(bankAccount);
            }

            // Check if the first transaction is a withdrawal
            if (type.ToUpper() == "W" && CalculateBalance(bankAccount.Transactions) == 0)
            {
                Console.WriteLine();
                Console.WriteLine("The first transaction on an account should not be a withdrawal.");
                return;
            }

            // Check if the transaction will cause the balance to go below 0
            if (type.ToUpper() == "W" && (CalculateBalance(bankAccount.Transactions) - amount) < 0)
            {
                Console.WriteLine();
                Console.WriteLine("The transaction will cause the balance to go below 0. Please try again.");
                return;
            }

            // Create a new transaction object
            TransactionDTO transaction = new TransactionDTO
            {
                Date = date,
                Account = account,
                Type = type.ToUpper(),
                Amount = amount
            };

            bankAccount.Transactions.Add(transaction);

            // Filter transactions with the same date as the current transaction
            var filteredTransactions = bankAccount.Transactions.Where(x => x.Date == transaction.Date).ToList();

            // Generate a transaction ID based on the date and the count of transactions with the same date
            string transactionId = date.Substring(2, 6) + "-" + (filteredTransactions.Count).ToString("D2");

            transaction.TransactionId = transactionId;

            Console.WriteLine();
            Console.WriteLine("Transaction added successfully.");

            // Print the account statement for the bank account.
            PrintAccountStatement(bankAccount);
        }

        public void DefineInterestRules()
        {
            // Prompt the user to enter interest rule details
            Console.WriteLine();
            Console.WriteLine("Please enter interest rule details in <Date>|<RuleId>|<Rate in %> format");
            Console.WriteLine("(or enter blank to go back to the main menu):");
            Console.Write("> ");

            string input = Console.ReadLine().Trim();

            // If input is blank, return to the main menu
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            string[] ruleDetails = input.Split('|');

            // Check if input has the correct number of parts
            if (ruleDetails.Length != 3)
            {
                Console.WriteLine();
                Console.WriteLine("Invalid input format. Please try again.");
                return;
            }

            string date = ruleDetails[0];
            string ruleId = ruleDetails[1];
            decimal rate;

            // Validate the date format
            if (!IsValidDateFormat(date))
            {
                Console.WriteLine();
                Console.WriteLine("Invalid date format. Please enter a valid date.");
                return;
            }

            if (!decimal.TryParse(ruleDetails[2], out rate))
            {
                Console.WriteLine();
                Console.WriteLine("Invalid rate. Please enter a valid decimal number.");
                return;
            }

            // Check if the interest rate is within the valid range (greater than 0 and less than 100)
            if (rate <= 0 || rate >= 100)
            {
                Console.WriteLine();
                Console.WriteLine("Rate should be greater than 0 and less than 100.");
                return;
            }

            // Create a new interest rule object
            InterestRuleDTO rule = new InterestRuleDTO
            {
                Date = date,
                RuleId = ruleId,
                Rate = rate
            };

            InterestRules.Add(rule);

            // Add the interest rule to the list of rules, replacing any existing rule for the same date
            InterestRules.RemoveAll(r => r.Date == date);
            InterestRules.Add(rule);

            Console.WriteLine();
            Console.WriteLine("Interest rule added successfully.");

            // Print the current interest rules
            PrintInterestRules();
        }

        public void PrintStatement()
        {
            // Prompt the user to enter account and month to generate the statement
            Console.WriteLine();
            Console.WriteLine("Please enter account and month to generate the statement <Account>|<Month>");
            Console.WriteLine("(or enter blank to go back to the main menu):");
            Console.Write("> ");

            string input = Console.ReadLine().Trim();

            // If input is blank, return to the main menu
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            string[] inputParts = input.Split('|');

            // Check if input has the correct number of parts
            if (inputParts.Length != 2)
            {
                Console.WriteLine();
                Console.WriteLine("Invalid input format. Please try again.");
                return;
            }

            string accountNumber = inputParts[0];
            string month = inputParts[1];

            // Find the bank account associated with the provided account number
            BankAccountDTO bankAccount = BankAccounts.Find(acc => acc.AccountNumber == accountNumber);

            if (bankAccount == null)
            {
                Console.WriteLine();
                Console.WriteLine("Account not found. Please enter a valid account number.");
                return;
            }

            // Generate and print the statement for the specified month
            PrintStatement(month, InterestRules, bankAccount.AccountNumber, bankAccount.Transactions);
        }

        public void Quit()
        {
            Console.WriteLine();
            Console.WriteLine("Thank you for banking with AwesomeGIC Bank.");
            Console.WriteLine("Have a nice day!");
        }

        public bool IsValidDateFormat(string inputDate)
        {
            // Check if the input date has the correct length
            if (inputDate.Length != 8)
            {
                return false;
            }

            if (!DateTime.TryParseExact(inputDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                return false;
            }

            // Format the parsed date back to the same format and compare it with the input date
            string formattedInputDate = parsedDate.ToString("yyyyMMdd");
            if (formattedInputDate != inputDate)
            {
                return false;
            }

            return true;
        }

        public void PrintAccountStatement(BankAccountDTO bankAccount)
        {
            // Print the account number
            Console.WriteLine();
            Console.WriteLine("Account: " + bankAccount.AccountNumber);

            // Print the table header for the statement
            Console.WriteLine("Date     | Txn Id    | Type | Amount |");

            // Iterate over each transaction in the bank account's list of transactions
            foreach (var transaction in bankAccount.Transactions)
            {
                // Print the details of each transaction in a formatted manner
                Console.WriteLine($"{transaction.Date} | {transaction.TransactionId} | {transaction.Type}    | {transaction.Amount,6:F2} |");
            }
        }

        public void PrintInterestRules()
        {
            Console.WriteLine();
            Console.WriteLine("Interest rules:");

            // Print the header for the interest rules section
            Console.WriteLine("Date     | RuleId | Rate (%)");

            // Iterate over each interest rule in the rules list
            foreach (var rule in InterestRules)
            {
                // Print the details of each interest rule in a formatted manner
                Console.WriteLine($"{rule.Date} | {rule.RuleId} | {rule.Rate,8:F2}");
            }
        }

        public decimal CalculateBalance(List<TransactionDTO> transactions)
        {
            decimal balance = 0;

            // Iterate over each transaction in the provided list of transactions
            foreach (var transaction in transactions)
            {
                // Check the transaction type to determine whether it is a deposit or withdrawal
                if (transaction.Type.ToUpper() == "D")
                {
                    // Add the transaction amount to the balance for deposits
                    balance += transaction.Amount;
                } 
                else if (transaction.Type.ToUpper() == "W")
                {
                    // Subtract the transaction amount from the balance for withdrawals
                    balance -= transaction.Amount;
                }
            }

            return balance;
        }

        
        public void PrintStatement(string month, List<InterestRuleDTO> rules, string accountNumber, List<TransactionDTO> transactions)
        {
            Console.WriteLine("Account: " + accountNumber);

            // Print the table header for the statement
            Console.WriteLine("Date     | Txn Id    | Type | Amount | Balance |");

            decimal balance = 0;
            int intMonth = 0;
            int index = 0;

            // Iterate over each transaction in the provided list of transactions
            foreach (var transaction in transactions)
            {
                // Update the balance based on the transaction type
                if (transaction.Type.ToUpper() == "D")
                {
                    balance += transaction.Amount;
                }
                else if (transaction.Type.ToUpper() == "W")
                {
                    balance -= transaction.Amount;
                }

                // Check if the transaction's month matches the specified month
                if (transaction.Date.Substring(4, 2) == month)
                {
                    if (index == 0)
                    {
                        StartYear = Convert.ToInt32(transaction.Date.Substring(0, 4));
                        EndYear = Convert.ToInt32(transaction.Date.Substring(0, 4));
                    }
                    else
                    {
                        EndYear = Convert.ToInt32(transaction.Date.Substring(0, 4));
                    }

                    // Print the transaction details in a formatted manner
                    Console.WriteLine($"{transaction.Date} | {transaction.TransactionId} | {transaction.Type}    | {transaction.Amount,6:F2} | {balance,7:F2} |");

                    index++;
                }
            }

            intMonth = Convert.ToInt32(month);
            decimal interest = 0;
            decimal interestWithBalance = CalculateInterestWithAnnualization(intMonth, rules, transactions, ref interest);

            // Get the last day of the specified month
            DateTime lastDayOfMonth = new DateTime(StartYear, intMonth, DateTime.DaysInMonth(StartYear, intMonth));
            string date = lastDayOfMonth.ToString("yyyyMMdd");

            // Print the interest transaction line in the statement
            Console.WriteLine($"{date} |           | I    | {interest,6:F2} | {interestWithBalance,7:F2} |");
        }

        public decimal CalculateInterestWithAnnualization(int targetMonth, List<InterestRuleDTO> rules, List<TransactionDTO> transactions, ref decimal finalAnnualizedInterest)
        {
            // Get the last day of the specified month
            DateTime lastDayOfMonth = new DateTime(StartYear, targetMonth, DateTime.DaysInMonth(StartYear, targetMonth));
            int lastday = lastDayOfMonth.Day;

            // Define the start and end dates for the interest calculation period
            DateTime startDate = new DateTime(StartYear, 6, 1);
            DateTime endDate = new DateTime(EndYear, 6, lastday);

            decimal accumulatedInterest = 0;
            finalAnnualizedInterest = 0;
            decimal calculatedBalance = 0;

            // Iterate over each date within the interest calculation period
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Calculate the balance for the current date
                decimal balanceToDate = CalculateBalanceToDate(date, transactions);
                calculatedBalance = balanceToDate;

                // Find the applicable interest rule for the current date
                var applicableRule = FindApplicableInterestRule(date, rules);

                // Calculate the number of days between the current date and the next day
                int numberOfDays = (date.AddDays(1) - date).Days;

                // Calculate the interest for the current date based on the balance and interest rate
                var calculatedInterest = balanceToDate * (applicableRule.Rate / 100) * numberOfDays;

                // Accumulate the interest for the current date
                accumulatedInterest += calculatedInterest;
            }

            // Calculate the final annualized interest by dividing the accumulated interest by 365
            finalAnnualizedInterest = accumulatedInterest / 365;

            // Add the final annualized interest to the calculated balance
            calculatedBalance += finalAnnualizedInterest;

            return calculatedBalance;
        }

        public InterestRuleDTO FindApplicableInterestRule(DateTime targetDate, List<InterestRuleDTO> rules)
        {
            // Iterate over each rule in descending order based on the date
            foreach (var rule in rules.OrderByDescending(x => x.Date))
            {
                DateTime ruleDate;
                string dateFormat = "yyyyMMdd";

                DateTime.TryParseExact(rule.Date, dateFormat, null, DateTimeStyles.None, out ruleDate);

                // Check if the rule's date is less than or equal to the end date
                if (ruleDate <= targetDate)
                {
                    return rule;
                    break;
                }
            }

            return null;
        }

        public decimal CalculateBalanceToDate(DateTime targetDate, List<TransactionDTO> transactions)
        {
            decimal balance = 0;

            // Iterate over each transaction in the provided list of transactions
            foreach (var transaction in transactions)
            {
                DateTime transactionDate;
                string dateFormat = "yyyyMMdd";

                DateTime.TryParseExact(transaction.Date, dateFormat, null, DateTimeStyles.None, out transactionDate);

                // Check if the transaction's date is less than or equal to the end date
                if (transactionDate <= targetDate)
                {
                    // Update the balance based on the transaction type
                    if (transaction.Type == "D")
                    {
                        balance += transaction.Amount;
                    }
                    else if (transaction.Type == "W")
                    {
                        balance -= transaction.Amount;
                    }
                }
            }

            return balance;
        }

    }

}
