namespace SAL.API
{
    public class ConfigurationSectionChangedEventArgs
    {
        public string SectionName { get; private set; }


        public ConfigurationSectionChangedEventArgs(string name)
        {
            SectionName = name;
        }
    }
}