using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace RttTest
{
    public static class RttTestUtilities
    {
        public const string Header = "[RTT]";
        
        public enum Role
        {
            Merger,
            Renderer,
            Server
        }

        public enum Action
        {
            Input,
            GhostArrived,
            BeginEncodeYuv,
            BeginDecodeYuv,
            BeginReadBack,
            FinishReadBack,
            SendFrame,
            ReceiveFrame,
            CombineFrame,
            Present
        }
        
        [Conditional("RTT_TEST")]
        public static void LogInput(Role role, uint tick)
        {
            LogRtt(role, tick, Action.Input);
        }
        
        [Conditional("RTT_TEST")]
        public static void LogGhostArrived(Role role, uint tick, int rendererId, uint triggerTick)
        {
            Debug.Log($"{Prefix(role, tick, rendererId)} {Action.GhostArrived} {triggerTick}");
        }
        
        [Conditional("RTT_TEST")]
        public static void BeginEncodeYuv(Role role, uint tick, int rendererId)
        {
            LogRtt(role, tick, Action.BeginEncodeYuv, rendererId);
        }
        
        [Conditional("RTT_TEST")]
        public static void BeginDecodeYuv(Role role, uint tick, int rendererId)
        {
            LogRtt(role, tick, Action.BeginDecodeYuv, rendererId);
        }
        
        [Conditional("RTT_TEST")]
        public static void BeginReadBack(Role role, uint tick, int rendererId)
        {
            LogRtt(role, tick, Action.BeginReadBack, rendererId);
        }
        
        [Conditional("RTT_TEST")]
        public static void FinishReadBack(Role role, uint tick, int rendererId)
        {
            LogRtt(role, tick, Action.FinishReadBack, rendererId);
        }
        
        [Conditional("RTT_TEST")]
        public static void SendFrame(Role role, uint tick, int rendererId)
        {
            LogRtt(role, tick, Action.SendFrame, rendererId);
        }
        
        [Conditional("RTT_TEST")]
        public static void ReceiveFrame(Role role, uint tick, int rendererId)
        {
            LogRtt(role, tick, Action.ReceiveFrame, rendererId);
        }
        
        [Conditional("RTT_TEST")]
        public static void CombineFrame(Role role, uint tick, int rendererId)
        {
            LogRtt(role, tick, Action.CombineFrame, rendererId);
        }
        
        [Conditional("RTT_TEST")]
        public static void LogRtt(Role role, uint tick, Action action, int id = 0)
        {
            Debug.Log($"{Prefix(role, tick, id)} {action}");
        }

        private static string Prefix(Role role, uint tick, int id = 0)
        {
            return $"{Header} {DateTime.UtcNow.Ticks} {tick} {role} {id}";
        }
    }
}