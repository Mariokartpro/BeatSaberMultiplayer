﻿using NSpeex;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BeatSaberMultiplayer.VOIP
{
    //https://github.com/DwayneBull/UnityVOIP/blob/master/AudioUtils.cs
    public static class AudioUtils
    {
        public static void ApplyGain(float[] samples, float gain)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= gain;
            }
        }

        public static float GetMaxAmplitude(float[] samples)
        {
            return samples.Max();
        }

        public static int GetFrequency( BandMode mode )
        {
            switch (mode)
            {
                case BandMode.Narrow:
                    return 8000;
                case BandMode.Wide:
                    return 16000;
                case BandMode.UltraWide:
                    return 32000;
                default:
                    return 8000;
            }
        }

        public static void Resample(float[] source, float[] target, int inputSampleRate, int outputSampleRate)
        {
            Resample(source, target, source.Length, target.Length, inputSampleRate, outputSampleRate);
        }

        public static void Resample(float[] source, float[] target, int inputNum, int outputNum, int inputSampleRate, int outputSampleRate)
        {
            float ratio = inputSampleRate / (float)outputSampleRate;
            if (ratio % 1 <= float.Epsilon)
            {
                int intRatio = (int)ratio;
                for (int i = 0; i < outputNum; i++)
                {
                    target[i] = source[i * intRatio];
                }
            }
            else
            {
                if (ratio > 1f)
                {
                    for (int i = 0; i < outputNum; i++)
                    {
                        target[i] = Mathf.Lerp(source[Mathf.FloorToInt(i * ratio)], source[Mathf.CeilToInt(i * ratio)], ratio % 1);
                    }
                }
                else
                {
                    for (int i = 0; i < outputNum; i++)
                    {
                        target[i] = source[Mathf.FloorToInt(i * ratio)];
                    }
                }
            }
        }

        public static int GetFreqForMic(string deviceName = null)
        {
            int minFreq;
            int maxFreq;

            Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
            
            if(minFreq >= 16000)
            {
                if(FindClosestFreq(minFreq, maxFreq) != 0)
                {
                    return FindClosestFreq(minFreq, maxFreq);
                }
                else
                {
                    return minFreq;
                }
            }
            else
            {
                return maxFreq;
            }
        }

        public static int[] possibleSampleRates = new int[] { 16000, 32000, 48000, 96000, 192000, 22050, 44100, 88200, 176400 };

        public static int FindClosestFreq(int minFreq, int maxFreq)
        {
            foreach(int sampleRate in possibleSampleRates)
            {
                if(sampleRate >= minFreq && sampleRate <= maxFreq)
                {
                    return sampleRate;
                }
            }
            return 0;
        }
    }
}