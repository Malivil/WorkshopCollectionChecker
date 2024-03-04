namespace WorkshopCollectionChecker
{
    internal class ConVarConflicts
    {
        public string AddonName { get; set; }
        public ConVarConflict[] Conflicts { get; set; }
    }

    internal class ConVarConflict
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Alternative { get; set; }
    }
}
