using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace HyperPay.Shared.Dtos
{
    public class DTOPaySlipReqInfo
    {
        public string PeriodId { get; set; }

        [Required(ErrorMessage = "From Date is required.")]
        [RegularExpression(@"^(((((0[1-9])|(1\d)|(2[0-8]))-((0[1-9])|(1[0-2])))|((31-((0[13578])|(1[02])))|((29|30)-((0[1,3-9])|(1[0-2])))))-((20[0-9][0-9]))|(29-02-20(([02468][048])|([13579][26]))))$", ErrorMessage = "Invalid from date format it's should be (dd-mm-yyyy).")]
        public string FromDate { get; set; }

        [Required(ErrorMessage = "To Date is required.")]
        [RegularExpression(@"^(((((0[1-9])|(1\d)|(2[0-8]))-((0[1-9])|(1[0-2])))|((31-((0[13578])|(1[02])))|((29|30)-((0[1,3-9])|(1[0-2])))))-((20[0-9][0-9]))|(29-02-20(([02468][048])|([13579][26]))))$", ErrorMessage = "Invalid to date format it's should be (dd-mm-yyyy).")]
        public string ToDate { get; set; }
       
        public string PayrollRunId { get; set; }
        public string RegionFrom { get; set; }
        public string RegionTo { get; set; }
        public string SectorFrom { get; set; }
        public string SectorTo { get; set; }
        public string DepartmentFrom { get; set; }
        public string DepartmentTo { get; set; }
        public string EmployeeId { get; set; }
        public string UserName { get; set; }
        public string Lang { get; set; } = "AR";
    }

    public class DTOPaySlipResInfo
    {
        public string RegionName { get; set; }
        public string SectorName { get; set; }
        public string DepartmentName { get; set; }
        public Employee Employee { get; set; }
    }

    public class Employee
    {
        public string EmployeeNumber { get; set; }
        public string EmployeeName { get; set; }
        public string OrganizationName { get; set; }
        public string Grade { get; set; }
        public string PositionName { get; set; }
        public string Nationality { get; set; }
        public string LocationName { get; set; }
        public string HireDate { get; set; }
        public string TerminationDate { get; set; }
        public string PeriodName { get; set; }
        public HashSet<DeductionElement> DeductionElements { get; set; }
        public HashSet<EarningElement> EarningElements { get; set; }
        public string TotalEarnings { get; set; }
        public string TotalDeductions { get; set; }
        public string NetValue { get; set; }
    }

    public class DeductionElement
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class EarningElement
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "EARNING_ELEMENT")]
    public class EARNING_ELEMENT
    {
        [XmlElement(ElementName = "ELEMENT_NAME")]
        public string ELEMENT_NAME { get; set; }
        [XmlElement(ElementName = "ELEMENT_VALUE")]
        public string ELEMENT_VALUE { get; set; }
    }

    [XmlRoot(ElementName = "DEDUCTION_ELEMENT")]
    public class DEDUCTION_ELEMENT
    {
        [XmlElement(ElementName = "ELEMENT_NAME")]
        public string ELEMENT_NAME { get; set; }
        [XmlElement(ElementName = "ELEMENT_VALUE")]
        public string ELEMENT_VALUE { get; set; }
    }

    [XmlRoot(ElementName = "EMPLOYEE")]
    public class EMPLOYEE
    {
        [XmlElement(ElementName = "EMPLOYEE_NUMBER")]
        public string EMPLOYEE_NUMBER { get; set; }
        [XmlElement(ElementName = "EMPLOYEE_NAME")]
        public string EMPLOYEE_NAME { get; set; }
        [XmlElement(ElementName = "ORG_NAME")]
        public string ORG_NAME { get; set; }
        [XmlElement(ElementName = "GRADE")]
        public string GRADE { get; set; }
        [XmlElement(ElementName = "POSITION_NAME")]
        public string POSITION_NAME { get; set; }
        [XmlElement(ElementName = "NATIONALITY")]
        public string NATIONALITY { get; set; }
        [XmlElement(ElementName = "LOCATION_NAME")]
        public string LOCATION_NAME { get; set; }
        [XmlElement(ElementName = "HIRE_DATE")]
        public string HIRE_DATE { get; set; }
        [XmlElement(ElementName = "TERMINATION_DATE")]
        public string TERMINATION_DATE { get; set; }
        [XmlElement(ElementName = "PERIOD_NAME")]
        public string PERIOD_NAME { get; set; }
        [XmlElement(ElementName = "EARNING_ELEMENT")]
        public List<EARNING_ELEMENT> EARNING_ELEMENT { get; set; }
        [XmlElement(ElementName = "DEDUCTION_ELEMENT")]
        public List<DEDUCTION_ELEMENT> DEDUCTION_ELEMENT { get; set; }
        [XmlElement(ElementName = "TOT_EARNINGS")]
        public string TOT_EARNINGS { get; set; }
        [XmlElement(ElementName = "TOT_DEDUCTIONS")]
        public string TOT_DEDUCTIONS { get; set; }
        [XmlElement(ElementName = "NET_VALUE")]
        public string NET_VALUE { get; set; }
    }

    [XmlRoot(ElementName = "G_DEPT")]
    public class G_DEPT
    {
        [XmlElement(ElementName = "DEPT_NAME")]
        public string DEPT_NAME { get; set; }
        [XmlElement(ElementName = "EMPLOYEE")]
        public EMPLOYEE EMPLOYEE { get; set; }
    }


    [XmlRoot(ElementName = "G_SECTOR")]
    public class G_SECTOR
    {
        [XmlElement(ElementName = "SECT_NAME")]
        public string SECT_NAME { get; set; }
        [XmlElement(ElementName = "G_DEPT")]
        public G_DEPT G_DEPT { get; set; }
    }

    [XmlRoot(ElementName = "G_REGION")]
    public class G_REGION
    {
        [XmlElement(ElementName = "REG_NAME")]
        public string REG_NAME { get; set; }
        [XmlElement(ElementName = "G_SECTOR")]
        public G_SECTOR G_SECTOR { get; set; }
    }

    [XmlRoot(ElementName = "G_REPORT")]
    public class G_REPORT
    {
        [XmlElement(ElementName = "P_PERIOD_NAME")]
        public string P_PERIOD_NAME { get; set; }
        [XmlElement(ElementName = "P_PAYROLL_RUN")]
        public string P_PAYROLL_RUN { get; set; }
        [XmlElement(ElementName = "P_FROM_REGION")]
        public string P_FROM_REGION { get; set; }
        [XmlElement(ElementName = "P_TO_REGION")]
        public string P_TO_REGION { get; set; }
        [XmlElement(ElementName = "P_FROM_SECTOR")]
        public string P_FROM_SECTOR { get; set; }
        [XmlElement(ElementName = "P_TO_SECTOR")]
        public string P_TO_SECTOR { get; set; }
        [XmlElement(ElementName = "P_FROM_DEPT")]
        public string P_FROM_DEPT { get; set; }
        [XmlElement(ElementName = "P_TO_DEPT")]
        public string P_TO_DEPT { get; set; }
        [XmlElement(ElementName = "P_FROM_EMP")]
        public string P_FROM_EMP { get; set; }
        [XmlElement(ElementName = "G_REGION")]
        public G_REGION G_REGION { get; set; }
    }
}
