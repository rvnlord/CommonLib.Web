namespace CommonLib.Web.Source.ViewModels
{
    public class TestEmployeeVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhotoPath { get; set; }
        public Dept? Department { get; set; }
        public string Domain { get; set; }
        public string Password { get; set; }
    }
    
    public enum Dept
    {
        None,
        IT,
        HR,
        Payroll
    }
}
