// SnippingTool.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

public class SnippingTool : Form
{
    private Rectangle _snipRegion;
    private static List<SnippingTool> _instances = new List<SnippingTool>();
    private Point _startPoint;
    private bool _selecting;

    public SnippingTool(Rectangle bounds)
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.Manual;
        this.Bounds = bounds;
        this.TopMost = true;
        this.DoubleBuffered = true;
        this.Cursor = Cursors.Cross;
        this.Opacity = .5;
        this.BackColor = Color.Magenta;
        this.TransparencyKey = this.BackColor;
        this._selecting = false;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _startPoint = e.Location;
        _selecting = true;
        _snipRegion = new Rectangle(e.X, e.Y, 0, 0);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_selecting) return;
        _snipRegion = new Rectangle(
            Math.Min(_startPoint.X, e.X),
            Math.Min(_startPoint.Y, e.Y),
            Math.Abs(e.X - _startPoint.X),
            Math.Abs(e.Y - _startPoint.Y)
        );
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _selecting = false;
        if (_snipRegion.Width > 0 && _snipRegion.Height > 0)
        {
            foreach (var instance in _instances)
            {
                if (instance != this)
                {
                    instance.Close();
                }
            }
            CaptureSnip();
        }
        else
        {
            foreach (var instance in _instances)
            {
                instance.Close();
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using (Brush tintBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
        {
            Region formRegion = new Region(this.ClientRectangle);

            formRegion.Exclude(_snipRegion);

            e.Graphics.FillRegion(tintBrush, formRegion);

            if (_selecting && _snipRegion.Width > 0 && _snipRegion.Height > 0)
            {
                e.Graphics.DrawRectangle(Pens.Red, _snipRegion);
            }
        }
    }


    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            foreach (var instance in _instances)
            {
                instance.Close();
            }
        }
    }

    private void CaptureSnip()
    {
        Bitmap bmp = new Bitmap(_snipRegion.Width, _snipRegion.Height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(new Point(_snipRegion.Left + this.Bounds.Left, _snipRegion.Top + this.Bounds.Top), Point.Empty, _snipRegion.Size);
        }
        Clipboard.SetImage(bmp);
        bmp.Dispose();
        this.Close();
    }

    public static void Snip()
    {
        foreach (Screen screen in Screen.AllScreens)
        {
            SnippingTool tool = new SnippingTool(screen.Bounds);
            _instances.Add(tool);
            tool.Show();
        }
    }
}
