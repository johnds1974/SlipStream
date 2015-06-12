using Accord.Extensions.Imaging;
using Accord.Imaging.Converters;
using Coligo.Core;
using Coligo.Platform;
using FFmpeg;
using FFmpeg.AutoGen;
using FFmpeg.AutoGen.Interop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SlipStream.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private MediaFile _mediaFile;
        private IVideoStream _videoStream;
        private IAudioStream _audioStream;
        private int _loadProgress;
        private bool _canLoadStream;
        private bool _canPlayStream;
        private bool _canStopStream;
        private bool _isLoaded;
        private bool _isPlaying;
        private string _loadMessage;
        private ObservableCollection<BitmapImage> _frames;
        private long _frameCount;
        private long _currentFrame;
//        private ImageSource _currentFrameImage;
        private ImageSource _currentFrameImage;
        private IntPtr _framePtr;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 
        /// </summary>
        public MainWindowViewModel()
        {
            Frames = new ObservableCollection<BitmapImage>();

            // register path to ffmpeg
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    string ffmpegPath = string.Format(@"../../ffmpeg/windows/{0}", Environment.Is64BitProcess ? "x64" : "x86");
                    InteropHelper.RegisterLibrariesSearchPath(ffmpegPath);
                    break;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<BitmapImage> Frames
        {
            get
            {
                return _frames;
            }
            set
            {
                if (_frames != value)
                {
                    _frames = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public long FrameCount
        {
            get
            {
                return _frameCount;
            }
            set
            {
                if (_frameCount != value)
                {
                    _frameCount = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double FrameRate
        {
            get
            {
                return _videoStream != null ? _videoStream.FrameRate : 0.0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int FrameWidth
        {
            get
            {
                return _videoStream != null ? _videoStream.Width : 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int FrameHeight
        {
            get
            {
                return _videoStream != null ? _videoStream.Height : 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public long CurrentFrame
        {
            get
            {
                return _currentFrame;
            }
            set
            {
                if (_currentFrame != value)
                {
                    _currentFrame = value;
                    OnPropertyChanged();
                    
                    // Asynchronously load this next frame...
//                    LoadCurrentFrameAsync();
                    LoadCurrentFrame();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ImageSource CurrentFrameImage
        {
            get
            {
                return _currentFrameImage;
            }
            private set
            {
                if (_currentFrameImage != value)
                {
                    _currentFrameImage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int LoadProgress
        {
            get
            {
                return _loadProgress;
            }
            set
            {
                if (_loadProgress != value)
                {
                    _loadProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string LoadMessage
        {
            get
            {
                return _loadMessage;
            }
            set
            {
                if (_loadMessage != value)
                {
                    _loadMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CanLoadStream
        {
            get
            {
                return !IsPlaying;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CanPlayStream
        {
            get
            {
                return IsLoaded && !IsPlaying;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CanStopStream
        {
            get
            {
                return IsPlaying;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return _isLoaded;
            }
            set
            {
                if (_isLoaded != value)
                {
                    _isLoaded = value;
                    this.Refresh();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public async void LoadStream()
        {
            await LoadStreamAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        public async void PlayStream()
        {
            await PlayStreamAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        public async void StopStream()
        {
            await StopStreamAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private void LoadCurrentFrame()
        {
            if (_videoStream != null)
            {
                // Convert the CurrentFrame to a TimeSpan...
                var millisecs = (CurrentFrame / (double)_videoStream.FrameRate) * 1000;
                TimeSpan ts = TimeSpan.FromMilliseconds(millisecs);

                try
                {
                    // Seek within our VideoStream...
                    var seekedts = _videoStream.Seek(ts, SeekOrigin.Begin);

                    byte[] buffer;
                    if (_videoStream.ReadFrame(out buffer))
                    {
//                        Marshal.Copy(buffer, 0, _currentFrameImage.BackBuffer, buffer.Length);

                        // Check if we need to allocate an unmanaged-pointer to our buffer...
                        //if (_framePtr == IntPtr.Zero)
                        //{
                        //    _framePtr = Marshal.AllocHGlobal(buffer.Length);
                        //}

                        //// Copy the buffer to our pointer location...
                        //Marshal.Copy(buffer, 0, _framePtr, buffer.Length);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            //var bs = BitmapSource.Create(
                            //    _videoStream.Width,
                            //    _videoStream.Height,
                            //    72,
                            //    72,
                            //    PixelFormats.Rgb24,
                            //    BitmapPalettes.BlackAndWhite,
                            //    _framePtr,
                            //    buffer.Length,
                            //    _videoStream.Stride);

                            if (CurrentFrameImage == null)
                            {
                                CurrentFrameImage = new WriteableBitmap(
                                    _videoStream.Width,
                                    _videoStream.Height,
                                    72,
                                    72,
                                    PixelFormats.Rgb24,
                                    null
                                    );


                            }

                            ((WriteableBitmap)CurrentFrameImage).WritePixels(
                                new Int32Rect(0, 0, _videoStream.Width, _videoStream.Height),
                                buffer,
                                _videoStream.Stride,
                                0
                                );

                            ((WriteableBitmap)CurrentFrameImage).Lock();
                            ((WriteableBitmap)CurrentFrameImage).AddDirtyRect(
                                new Int32Rect(0, 0, _videoStream.Width, _videoStream.Height));
                            ((WriteableBitmap)CurrentFrameImage).Unlock();

                        });

                        //OnUIThread(o =>
                        //{
                        //    var bs = BitmapSource.Create(
                        //        _videoStream.Width,
                        //        _videoStream.Height,
                        //        72,
                        //        72,
                        //        PixelFormats.Rgb24,
                        //        BitmapPalettes.BlackAndWhite,
                        //        _framePtr,
                        //        buffer.Length,
                        //        _videoStream.Stride);

                        //    CurrentFrameImage = bs;
                        //},
                        //null
                        //);

                    }

                    // Seek within our AudoStream to the same place as Video...
                    seekedts = _audioStream.Seek(ts, SeekOrigin.Begin);

                    byte[] audioBuffer;
                    if (_audioStream.ReadSample(out audioBuffer))
                    {

                    }

                }
                catch (Exception ex)
                {

                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task LoadCurrentFrameAsync()
        {
            if (_videoStream != null)
            {
                // Convert the CurrentFrame to a TimeSpan...
                var millisecs = (CurrentFrame / (double)_videoStream.FrameRate) * 1000;
                TimeSpan ts = TimeSpan.FromMilliseconds(millisecs);

                try
                {
                    // Seek within our VideoStream...
                    var seekedts = _videoStream.Seek(ts, SeekOrigin.Begin);

                    byte[] buffer;
                    if (_videoStream.ReadFrame(out buffer))
                    {
                        // Check if we need to allocate an unmanaged-pointer to our buffer...
                        if (_framePtr == IntPtr.Zero)
                        {
                            _framePtr = Marshal.AllocHGlobal(buffer.Length);
                        }

                        // Copy the buffer to our pointer location...
                        Marshal.Copy(buffer, 0, _framePtr, buffer.Length);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var bs = BitmapSource.Create(
                                _videoStream.Width,
                                _videoStream.Height,
                                72,
                                72,
                                PixelFormats.Rgb24,
                                BitmapPalettes.BlackAndWhite,
                                _framePtr,
                                buffer.Length,
                                _videoStream.Stride);

                            CurrentFrameImage = bs;
                        });

                        //OnUIThread(o =>
                        //{
                        //    var bs = BitmapSource.Create(
                        //        _videoStream.Width,
                        //        _videoStream.Height,
                        //        72,
                        //        72,
                        //        PixelFormats.Rgb24,
                        //        BitmapPalettes.BlackAndWhite,
                        //        _framePtr,
                        //        buffer.Length,
                        //        _videoStream.Stride);

                        //    CurrentFrameImage = bs;
                        //},
                        //null
                        //);

                    }

                    // Seek within our AudoStream to the same place as Video...
                    seekedts = _audioStream.Seek(ts, SeekOrigin.Begin);

                    byte[] audioBuffer;
                    if (_audioStream.ReadSample(out audioBuffer))
                    {

                    }

                }
                catch (Exception ex)
                {

                }
            }

//                    var frame = _reader.Seek(CurrentFrame, SeekOrigin.Begin);
//                    var image = _reader.Read() as Image<Bgr<byte>>;

/*
                    if (image != null)
                    {
                        MemoryStream ms = new MemoryStream();
                        var bitmap = image.AsBitmap();
                        bitmap.Save(ms, ImageFormat.Bmp);

                        _currentFrameImage = new BitmapImage();
                        _currentFrameImage.BeginInit();
                        ms.Position = 0;
                        _currentFrameImage.StreamSource = ms;
                        _currentFrameImage.EndInit();
                    }
*/

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task LoadStreamAsync()
        {
            OpenFileDialog fileOpenDialog = new OpenFileDialog();
            fileOpenDialog.Multiselect = false;

            if (fileOpenDialog.ShowDialog() == true)
            {
                if (_mediaFile != null)
                {
                    _mediaFile.Dispose();
                    IsLoaded = false;
                }

                string fileName = fileOpenDialog.FileName;

                await Task.Run(() =>
                {

                    try
                    {
                        // Free our unmanaged pointer if it is already allocated...
                        if (_framePtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(_framePtr);
                            _framePtr = IntPtr.Zero;
                        }

                        _mediaFile = new MediaFile(fileName);

                        VideoDecoderStream videoStream = (VideoDecoderStream)_mediaFile.Streams.FirstOrDefault(s => s is VideoDecoderStream);

                        if (videoStream != null)
                        {
                            _videoStream = new VideoScalingStream(
                                videoStream,
                                videoStream.Width,
                                videoStream.Height,
                                AVPixelFormat.AV_PIX_FMT_RGB24);

                            FrameCount = videoStream.FrameCount;
                        }

                        _audioStream = (AudioDecoderStream)_mediaFile.Streams.FirstOrDefault(s => s is AudioDecoderStream);

                        CurrentFrame = 0;
                        IsLoaded = true;
                    }
                    catch (Exception ex)
                    {

                    }

                });
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task StopStreamAsync()
        {
            // Request cancellation...
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task PlayStreamAsync()
        {
            if (_cancellationTokenSource == null)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            Task.Run(async () =>
                {
                    IsPlaying = true;

                    // Play each frame, until we are cancelled, or reached the end...
                    while (!_cancellationTokenSource.IsCancellationRequested && CurrentFrame <= FrameCount)
                    {
                        // Increment the current frame, which will load the actual frame image...
                        CurrentFrame++;

                        // Wait around for the FrameRate interval...
                        int interval = 500;// (int)(1000 / FrameRate);

                        await Task.Delay(interval);
                    }

                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Dispose();
                        _cancellationTokenSource = null;
                    }

                    IsPlaying = false;
                },
                _cancellationTokenSource.Token
            );
        }

    }
}
