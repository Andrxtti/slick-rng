namespace Dream
{
    // The hand-off between the editor's render command and whatever plays in the scene: which film to play, and when
    // it is over. Film reads Id on start and sets Done at the end; the recorder stops on Done.
    public static class Take
    {
        public static string Id;
        public static bool Done;
        public static bool Recording;
    }
}
