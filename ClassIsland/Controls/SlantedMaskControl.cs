using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace ClassIsland.Controls;

public class SlantedMaskControl : Control
{
    // 填充色
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<SlantedMaskControl, IBrush?>(nameof(Fill), Brushes.Black);

    // 平行四边形高度
    public static readonly StyledProperty<double> SlantedHeightProperty =
        AvaloniaProperty.Register<SlantedMaskControl, double>(nameof(SlantedHeight), 80.0);

    // 动画参数（毫秒）
    public static readonly StyledProperty<int> RegionDurationMsProperty =
        AvaloniaProperty.Register<SlantedMaskControl, int>(nameof(RegionDurationMs), 300);

    public static readonly StyledProperty<int> StageStaggerMsProperty =
        AvaloniaProperty.Register<SlantedMaskControl, int>(nameof(StageStaggerMs), 120);

    // 五个可动画进度属性（StyledProperty -> 可由 Animation 驱动）
    public static readonly StyledProperty<double> Region0ProgressProperty =
        AvaloniaProperty.Register<SlantedMaskControl, double>(nameof(Region0Progress), 0.0);

    public static readonly StyledProperty<double> Region1ProgressProperty =
        AvaloniaProperty.Register<SlantedMaskControl, double>(nameof(Region1Progress), 0.0);

    public static readonly StyledProperty<double> Region2ProgressProperty =
        AvaloniaProperty.Register<SlantedMaskControl, double>(nameof(Region2Progress), 0.0);

    public static readonly StyledProperty<double> Region3ProgressProperty =
        AvaloniaProperty.Register<SlantedMaskControl, double>(nameof(Region3Progress), 0.0);

    public static readonly StyledProperty<double> Region4ProgressProperty =
        AvaloniaProperty.Register<SlantedMaskControl, double>(nameof(Region4Progress), 0.0);

    public static readonly StyledProperty<bool> IsOpenedProperty = AvaloniaProperty.Register<SlantedMaskControl, bool>(
        nameof(IsOpened));

    public bool IsOpened
    {
        get => GetValue(IsOpenedProperty);
        set => SetValue(IsOpenedProperty, value);
    }

    // 属性封装
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public double SlantedHeight
    {
        get => GetValue(SlantedHeightProperty);
        set => SetValue(SlantedHeightProperty, value);
    }

    public int RegionDurationMs
    {
        get => GetValue(RegionDurationMsProperty);
        set => SetValue(RegionDurationMsProperty, value);
    }

    public int StageStaggerMs
    {
        get => GetValue(StageStaggerMsProperty);
        set => SetValue(StageStaggerMsProperty, value);
    }

    public double Region0Progress
    {
        get => GetValue(Region0ProgressProperty);
        set => SetValue(Region0ProgressProperty, value);
    }

    public double Region1Progress
    {
        get => GetValue(Region1ProgressProperty);
        set => SetValue(Region1ProgressProperty, value);
    }

    public double Region2Progress
    {
        get => GetValue(Region2ProgressProperty);
        set => SetValue(Region2ProgressProperty, value);
    }

    public double Region3Progress
    {
        get => GetValue(Region3ProgressProperty);
        set => SetValue(Region3ProgressProperty, value);
    }

    public double Region4Progress
    {
        get => GetValue(Region4ProgressProperty);
        set => SetValue(Region4ProgressProperty, value);
    }

    // 内部 cancellation 用于中断动画
    private CancellationTokenSource? _cts;

    // 固定 60°
    private static readonly double Tan30 = Math.Tan(Math.PI / 3.0);

    public SlantedMaskControl()
    {
        // 初始化为 closed
        Region0Progress = Region1Progress = Region2Progress = Region3Progress = Region4Progress = 0.0;

        this.GetObservable(Region0ProgressProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(Region1ProgressProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(Region2ProgressProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(Region3ProgressProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(Region4ProgressProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(IsOpenedProperty).Skip(1).Subscribe(_ =>
        {
            if (IsOpened)
            {
                Open();
            }
            else
            {
                Close();
            }
        });
    }

    #region 公共 API：Open / Close

    /// <summary>打开遮罩（两边 -> 中间）</summary>
    public async void Open()
    {
        await StartSequence(open: true).ConfigureAwait(false);
    }

    /// <summary>关闭遮罩（中间 -> 两边）</summary>
    public async void Close()
    {
        await StartSequence(open: false).ConfigureAwait(false);
    }

    private async Task StartSequence(bool open)
    {
        // 取消上一次动画
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // 参数
        int dur = Math.Max(1, RegionDurationMs);
        int stag = Math.Max(0, StageStaggerMs);

        // 为每个 region 构建 Animation（带 Delay）
        // 我们按阶段安排开始时间：open: outer(0,4) start 0, inner(1,3) start stag, center(2) start 2*stag
        // close: reverse（center first）
        var tasks = new List<Task>();

        try
        {
            if (open)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Region0Progress = Region1Progress = Region2Progress = Region3Progress = Region4Progress = 0;
                });
                tasks.Add(RunRegionAnim(0, 0, dur, true, token));
                tasks.Add(RunRegionAnim(4, 0, dur, true, token));

                tasks.Add(RunRegionAnim(1, (int)(stag * 0.75), dur, true, token));
                tasks.Add(RunRegionAnim(3, (int)(stag * 0.75), dur, true, token));

                tasks.Add(RunRegionAnim(2, (int)(stag * 1), dur, true, token));
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Region0Progress = Region1Progress = Region2Progress = Region3Progress = Region4Progress = 1;
                });
                tasks.Add(RunRegionAnim(2, 0, dur, false, token));

                tasks.Add(RunRegionAnim(1, (int)(stag * 0.75), dur, false, token));
                tasks.Add(RunRegionAnim(3, (int)(stag * 0.75), dur, false, token));

                tasks.Add(RunRegionAnim(0, (int)(stag * 1), dur, false, token));
                tasks.Add(RunRegionAnim(4, (int)(stag * 1), dur, false, token));
            }

            // 等待所有动画完成（或被取消）
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // 忽略取消
        }
        catch (OperationCanceledException)
        {
            // 忽略取消
        }
        finally
        {
            // 最终修正值（避免插值残留）
            if (!token.IsCancellationRequested)
            {
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    double finish = open ? 1.0 : 0.0;
                    Region0Progress = Region1Progress = Region2Progress = Region3Progress = Region4Progress = finish;
                });
            }
        }

        return;
        
        double DoCubicOut(double x) => 1 - Math.Pow(1 - x, 3);
    }

    /// <summary>
    /// 为单个 region 运行动画：在 delayMs 后开始，从 0->1（open）或 1->0（close）
    /// 通过 Avalonia.Animation.Animation 去插值驱动对应的 StyledProperty。
    /// </summary>
    private async Task RunRegionAnim(int regionIndex, int delayMs, int durationMs, bool forward,
        CancellationToken token)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var animation = new Animation
            {
                Delay = TimeSpan.FromMilliseconds(delayMs),
                Duration = TimeSpan.FromMilliseconds(durationMs),
                FillMode = FillMode.Forward,
                Easing = forward ? new QuarticEaseOut() : new QuadraticEaseIn()// 如果你的 Avalonia 版本没有 Easing 属性，可以移除或改为适配的写法
            };

            // 目标属性与值
            var targetProperty = GetRegionProperty(regionIndex);
            double from = forward ? 0.0 : 1.0;
            double to = forward ? 1.0 : 0.0;

            // 创建 KeyFrames（通过 Setter 设置属性值）
            var kf0 = new KeyFrame
            {
                Cue = new Cue(0d),
                Setters = { new Setter(targetProperty, from) }
            };
            var kf1 = new KeyFrame
            {
                Cue = new Cue(1d),
                Setters = { new Setter(targetProperty, to) }
            };

            animation.Children.Add(kf0);
            animation.Children.Add(kf1);
            
            await animation.RunAsync(this, token).ConfigureAwait(false);
        });
    }

    // 辅助：根据 index 返回对应的 StyledProperty<double>
    private static AvaloniaProperty<double> GetRegionProperty(int idx)
    {
        return idx switch
        {
            0 => Region0ProgressProperty,
            1 => Region1ProgressProperty,
            2 => Region2ProgressProperty,
            3 => Region3ProgressProperty,
            4 => Region4ProgressProperty,
            _ => throw new ArgumentOutOfRangeException(nameof(idx))
        };
    }

    #endregion

    #region 绘制

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var size = Bounds;
        if (size.Width <= 0 || size.Height <= 0) return;

        double h = SlantedHeight;
        if (h <= 0) h = size.Height;
        if (h > size.Height) h = size.Height;
        double offset = Math.Tan(Math.PI / 6.0) * h;

        double totalW = size.Width + offset;

        // 读取进度值（从属性）
        double[] p =
        [
            Math.Clamp(Region0Progress, 0.0, 1.0),
            Math.Clamp(Region1Progress, 0.0, 1.0),
            Math.Clamp(Region2Progress, 0.0, 1.0),
            Math.Clamp(Region3Progress, 0.0, 1.0),
            Math.Clamp(Region4Progress, 0.0, 1.0)
        ];

        double[] weight = [0.0, 0.1, 0.2, 0.4, 0.2, 0.1];
        double[] startX =
        [
            totalW * weight[0],
            totalW * weight[1] + totalW * weight[0],
            totalW * weight[2] + totalW * weight[1] + totalW * weight[0],
            totalW * weight[3] + totalW * weight[2] + totalW * weight[1] + totalW * weight[0],
            totalW * weight[4] + totalW * weight[3] + totalW * weight[2] + totalW * weight[1] + totalW * weight[0],
            totalW * weight[5] + totalW * weight[4] + totalW * weight[3] + totalW * weight[2] + totalW * weight[1] +
            totalW * weight[0],
        ];

        var brush = Fill ?? Brushes.Black;

        for (int i = 0; i < 5; i++)
        {
            double prog = p[i];
            if (prog < 0.0) continue;

            double rx0 = startX[i];
            double rx1 = startX[i + 1];
            double rCenter = (rx0 + rx1) / 2.0;

            double fullW = totalW * weight[i + 1] + 0.5;
            double currentW = fullW * prog;
            if (currentW < 0.0001) continue;
            
            var p1 = new Point(rCenter - currentW / 2.0, 0);
            var p2 = new Point(rCenter + currentW / 2.0, 0);
            var p3 = new Point(rCenter + currentW / 2.0 - offset, h);
            var p4 = new Point(rCenter - currentW / 2.0 - offset, h);

            var geom = new StreamGeometry();
            using (var g = geom.Open())
            {
                g.BeginFigure(p1, isFilled: true);
                g.LineTo(p2);
                g.LineTo(p3);
                g.LineTo(p4);
                g.LineTo(p1);
                g.EndFigure(isClosed: true);
            }

            context.DrawGeometry(brush, null, geom);
        }
    }

    #endregion
}