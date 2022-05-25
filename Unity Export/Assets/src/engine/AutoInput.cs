using System;
using System.Collections.Generic;
using System.Diagnostics;

#if WINDOWS && DEBUG
namespace Yukar.Engine
{
    class AutoInput
    {
        private static AutoInput _instance = new AutoInput(); // 唯一のインスタンス
        private bool enable; // 実行するかどうか
        private List<Tuple<Input.KeyStates, bool, string>> jobs; // 実行するキーのリスト
        private int jobIndex; // 実行するキー
        private Input.KeyStates newJobKeyState; // 追加用
        private bool newJobUseAutoMove = false; // 追加用

        public enum MoveType
        {
            NONE = 0,
            UP,
            DOWN,
            LEFT,
            RIGHT,
            RANDOM,
        }

        AutoInput()
        {
            enable = false;
            jobs = new List<Tuple<Input.KeyStates, bool, string>>();
            jobIndex = 0;
            newJobKeyState = Input.KeyStates.NONE;
            newJobUseAutoMove = false;
        }

        public static AutoInput GetInstance()
        {
            return _instance;
        }

        [Conditional("DEBUG")]
        public void Update()
        {
            if (!enable)
            {
                return;
            }
            if (jobs.Count == 0)
            {
                return;
            }

            var keyState = jobs[jobIndex].Item1;
            var useAutoMove = jobs[jobIndex].Item2;
            var name = jobs[jobIndex].Item3;

            if (useAutoMove)
            {
                var allDirection = 0 | Input.KeyStates.UP | Input.KeyStates.DOWN | Input.KeyStates.RIGHT | Input.KeyStates.LEFT;
                keyState &= ~allDirection;
                keyState |= AutoMove();
            }
            Input.ChangeInput(keyState);
        }

        [Conditional("DEBUG")]
        public void Enable()
        {
            enable = true;
        }

        [Conditional("DEBUG")]
        public void Disable()
        {
            enable = false;
        }

        [Conditional("DEBUG")]
        public void ToggleEnable()
        {
            enable = !enable;
        }

        [Conditional("DEBUG")]
        public void DoNextJob()
        {
            if (jobIndex == jobs.Count - 1)
            {
                jobIndex = 0;
            }
            else
            {
                ++jobIndex;
            }
        }

        /// <summary>
        /// 指定したジョブに移動する
        /// </summary>
        /// <param name="index">移動したいジョブの配列番号</param>
        [Conditional("DEBUG")]
        public void JumpSelectJob(int index)
        {
            if (index < jobs.Count)
            {
                jobIndex = index;
            }
        }
        /// <summary>
        /// 指定したジョブに移動する
        /// </summary>
        /// <param name="jobName">移動したいジョブの名前</param>
        [Conditional("DEBUG")]
        public void JumpSelectJob(string jobName)
        {
            var searchResult = searchSameJobName(jobName);
            if (searchResult.Item1)
            {
                jobIndex = searchResult.Item2;
            }
        }

        [Conditional("DEBUG")]
        public void AddJob(Input.KeyStates keyState, bool useAutoMove, string jobName)
        {
            if (searchSameJobName(jobName).Item1)
            {
                return;
            }
            jobs.Add(new Tuple<Input.KeyStates, bool, string>(keyState, useAutoMove, jobName));
        }

        [Conditional("DEBUG")]
        public void RemoveJob(int index)
        {
            jobs.RemoveAt(index);
        }

        [Conditional("DEBUG")]
        public void ClearJobs()
        {
            jobs.Clear();
        }

        [Conditional("DEBUG")]
        public void AddNewJobKeyState(Input.KeyStates keyState)
        {
            if (keyState <= Input.KeyStates.RIGHT)
            {
                AddNewJobMove(MoveType.NONE);
            }
            newJobKeyState |= keyState;
        }

        [Conditional("DEBUG")]
        public void RemoveNewJobKeyState(Input.KeyStates keyState)
        {
            newJobKeyState &= ~keyState;
        }

        [Conditional("DEBUG")]
        public void AddNewJobMove(MoveType moveType)
        {
            // 一旦移動を削除
            var allDirection = 0 | Input.KeyStates.UP | Input.KeyStates.DOWN | Input.KeyStates.RIGHT | Input.KeyStates.LEFT;
            RemoveNewJobKeyState(allDirection);
            switch (moveType)
            {
                case MoveType.NONE:
                    break;
                case MoveType.UP:
                    newJobUseAutoMove = false;
                    AddNewJobKeyState(Input.KeyStates.UP);
                    break;
                case MoveType.DOWN:
                    newJobUseAutoMove = false;
                    AddNewJobKeyState(Input.KeyStates.DOWN);
                    break;
                case MoveType.LEFT:
                    newJobUseAutoMove = false;
                    AddNewJobKeyState(Input.KeyStates.LEFT);
                    break;
                case MoveType.RIGHT:
                    newJobUseAutoMove = false;
                    AddNewJobKeyState(Input.KeyStates.RIGHT);
                    break;
                case MoveType.RANDOM:
                    newJobUseAutoMove = true;
                    break;
                default:
                    break;
            }
            return;
        }

        [Conditional("DEBUG")]
        public void MakeNewJob(string jobName)
        {
            if (searchSameJobName(jobName).Item1)
            {
                return;
            }
            jobs.Add(new Tuple<Input.KeyStates, bool, string>(newJobKeyState, newJobUseAutoMove, jobName));
            ResetNewJobData();
        }

        [Conditional("DEBUG")]
        public void ResetNewJobData()
        {
            newJobKeyState = Input.KeyStates.NONE;
            newJobUseAutoMove = false;
        }

        private Input.KeyStates AutoMove()
        {
            Random rnd = new Random();
            var direction = rnd.Next(1, 5);
            return (Input.KeyStates)direction;
        }

        private Tuple<bool, int> searchSameJobName(string jobName)
        {
            for (var i = 0; i < jobs.Count; ++i)
            {
                if (jobName == jobs[i].Item3)
                {
                    return new Tuple<bool, int>(true, i);
                }
            }
            return new Tuple<bool, int>(false, -1);
        }
    }
}
#endif // #if WINDOWS && DEBUG