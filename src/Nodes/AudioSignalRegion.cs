﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VL.Core;
using VL.Lib.Animation;
using VL.Lib.Collections;

namespace VL.Audio
{
    public class AudioSignalRegion<TState, TIn, TOut> : IDisposable
       where TState : class
    {
        internal BufferCallerSignal PerBufferSignal;

        readonly Subject<TOut> FSubject;

        public AudioSignalRegion()
        {
            //signal
            PerBufferSignal = new BufferCallerSignal();
            FSubject = new Subject<TOut>();
        }

        Func<TState, TIn, AudioBufferStereo, Tuple<TState, TOut>> FUpdater;
        TOut FOutput;

        TState State;

        public Spread<AudioSignal> Update(IEnumerable<AudioSignal> stereoInput, TIn input, bool reset, Func<TState> create, Func<TState, TIn, AudioBufferStereo, Tuple<TState, TOut>> update, out IObservable<TOut> onBufferProcessed, out Time time)
        {
            if (reset || State == null)
                State = create();

            PerBufferSignal.SetInput(stereoInput);

            if (FUpdater != update)
            {
                PerBufferSignal.PerBuffer = (buffer) =>
                {
                    try
                    {
                        var result = FUpdater(State, input, buffer);
                        State = result.Item1;
                        FOutput = result.Item2;
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                    }
                };

                FUpdater = update;
            }

            time = PerBufferSignal.StereoBuffer.StartTime;
            
            if(FOutput != null)
                FSubject.OnNext(FOutput);
            onBufferProcessed = FSubject;
            return PerBufferSignal.Outputs.ToSpread();
        }

        public void Dispose()
        {
            var disposable = State as IDisposable;
            disposable?.Dispose();
            FSubject.Dispose();
        }
    }

    public class AudioSignalGeneratorRegion<TState, TIn, TOut> : IDisposable
      where TState : class
    {
        internal MultiBufferGeneratorSignal PerBufferSignal;

        readonly Subject<TOut> FSubject;

        public AudioSignalGeneratorRegion(int channelCount = 2)
        {
            //signal
            PerBufferSignal = new MultiBufferGeneratorSignal(channelCount);
            FSubject = new Subject<TOut>();
        }

        Func<TState, TIn, AudioBufferMulti, Tuple<TState, TOut>> FUpdater;
        TOut FOutput;

        TState State;

        public IReadOnlyList<AudioSignal> Update(TIn input, bool reset, Func<TState> create, Func<TState, TIn, AudioBufferMulti, Tuple<TState, TOut>> update, out IObservable<TOut> onBufferProcessed, out Time time)
        {
            if (reset || State == null)
                State = create();

            if (FUpdater != update)
            {
                PerBufferSignal.PerBuffer = (buffer) =>
                {
                    try
                    {
                        var result = FUpdater(State, input, buffer);
                        State = result.Item1;
                        FOutput = result.Item2;
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                    }
                };

                FUpdater = update;
            }

            time = PerBufferSignal.Buffer.StartTime;

            if (FOutput != null)
                FSubject.OnNext(FOutput);
            onBufferProcessed = FSubject;
            return PerBufferSignal.Outputs;
        }

        public void Dispose()
        {
            var disposable = State as IDisposable;
            disposable?.Dispose();
            FSubject.Dispose();
        }
    }
}
