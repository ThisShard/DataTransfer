using System.Drawing;
using LargeXlsx;

namespace ThisShard.Database.Infrastructure.Excel.Options;

public class ExcelStyleOptions
{
    /// <summary>
    /// Стиль заголовка
    /// </summary>
    public XlsxStyle HeaderStyle { get; set; } = XlsxStyle.Default.With(XlsxNumberFormat.Text)
        .With(XlsxBorder.Around(new XlsxBorder.Line(Color.Black, XlsxBorder.Style.Medium)));
    
    /// <summary>
    /// Стиль даты и времени
    /// </summary>
    public XlsxStyle DateTimeStyle { get; set; } = XlsxStyle.Default.With(XlsxNumberFormat.ShortDateTime)
        .With(XlsxBorder.Around(new XlsxBorder.Line(Color.Black, XlsxBorder.Style.Thin)));
    
    /// <summary>
    /// Стиль текста
    /// </summary>
    public XlsxStyle TextStyle { get; set; } = XlsxStyle.Default.With(XlsxNumberFormat.Text)
        .With(XlsxBorder.Around(new XlsxBorder.Line(Color.Black, XlsxBorder.Style.Thin)));
    
    /// <summary>
    /// Стиль числа
    /// </summary>
    public XlsxStyle IntegerStyle { get; set; } = XlsxStyle.Default.With(XlsxNumberFormat.Integer)
        .With(XlsxBorder.Around(new XlsxBorder.Line(Color.Black, XlsxBorder.Style.Thin)));
    
    /// <summary>
    /// Стиль десятичного числа
    /// </summary>
    public XlsxStyle DecimalStyle { get; set; } = XlsxStyle.Default.With(XlsxNumberFormat.General)
        .With(XlsxBorder.Around(new XlsxBorder.Line(Color.Black, XlsxBorder.Style.Thin)));
    
    /// <summary>
    /// Стиль экспоненциального числа
    /// </summary>
    public XlsxStyle DoubleStyle { get; set; } = XlsxStyle.Default.With(XlsxNumberFormat.General)
        .With(XlsxBorder.Around(new XlsxBorder.Line(Color.Black, XlsxBorder.Style.Thin)));
    
    /// <summary>
    /// Стиль булево поля
    /// </summary>
    public XlsxStyle BooleanStyle { get; set; } = XlsxStyle.Default.With(XlsxNumberFormat.General)
        .With(XlsxBorder.Around(new XlsxBorder.Line(Color.Black, XlsxBorder.Style.Thin)));
    
    /// <summary>
    /// Стиль пустого поля
    /// </summary>
    public XlsxStyle NullStyle { get; set; } = XlsxStyle.Default.With(XlsxNumberFormat.General)
        .With(XlsxBorder.Around(new XlsxBorder.Line(Color.Black, XlsxBorder.Style.Thin)));
}