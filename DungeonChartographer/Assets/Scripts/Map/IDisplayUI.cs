public interface IDisplayUI
{
    /// <summary>
    /// use %FunctionName, or text
    /// </summary>
    /// <param name="text"></param>
    void ShowText(string text);
    void Show(object data);
}
