using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimeStamp.GameFeatures
{
    /// <summary>
    /// 解谜系统主控制器
    /// </summary>
    public class PuzzleSystem : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private float puzzleDifficulty = 1.0f;
        [SerializeField] private int maxAttempts = 3;

        [Header("UI")]
        [SerializeField] private GameObject puzzlePanel;
        [SerializeField] private TextMeshProUGUI puzzleQuestionText;
        [SerializeField] private InputField playerAnswer;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button skipButton;

        private int currentPuzzleId = 0;
        private int attemptsLeft = 0;
        private List<Puzzle> puzzles = new List<Puzzle>();
        private bool isPuzzleActive = false;

        public static PuzzleSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializePuzzles();
            SetupUI();
        }

        private void InitializePuzzles()
        {
            // 初始化谜题数据
            puzzles.Add(new Puzzle
            {
                id = 1,
                type = PuzzleType.Historical,
                question = "《时光之痕》故事发生在哪一年？",
                answer = "2150",
                hint = "故事发生在未来",
                difficulty = 1
            });

            puzzles.Add(new Puzzle
            {
                id = 2,
                type = PuzzleType.TimeTravel,
                question = "时间线切换需要消耗多少失眠值？",
                answer = "20",
                hint = "参考游戏机制设计文档",
                difficulty = 1
            });

            puzzles.Add(new Puzzle
            {
                id = 3,
                type = PuzzleType.Logic,
                question = "林晓的身份是什么？",
                answer = "历史系研究生",
                hint = "查看角色设定文档",
                difficulty = 1
            });
        }

        private void SetupUI()
        {
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmitAnswer);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipPuzzle);
            }
        }

        public void StartPuzzle(int puzzleId)
        {
            if (puzzleId >= puzzles.Count || puzzleId < 0)
            {
                Debug.LogError($"无效的谜题ID: {puzzleId}");
                return;
            }

            currentPuzzleId = puzzleId;
            attemptsLeft = maxAttempts;
            isPuzzleActive = true;

            ShowPuzzle(puzzles[puzzleId]);
        }

        private void ShowPuzzle(Puzzle puzzle)
        {
            if (puzzlePanel != null)
            {
                puzzlePanel.SetActive(true);
            }

            if (puzzleQuestionText != null)
            {
                puzzleQuestionText.text = puzzle.question;
            }

            if (playerAnswer != null)
            {
                playerAnswer.text = "";
                playerAnswer.interactable = true;
            }

            Debug.Log($"[解谜] 谜题ID: {puzzle.id}, 问题: {puzzle.question}");
        }

        private void OnSubmitAnswer()
        {
            if (!isPuzzleActive)
            {
                return;
            }

            string playerAnswerText = playerAnswer.text.Trim();
            Puzzle currentPuzzle = puzzles[currentPuzzleId];

            if (string.IsNullOrEmpty(playerAnswerText))
            {
                Debug.LogWarning("[解谜] 请输入答案");
                return;
            }

            // 检查答案
            if (playerAnswerText.Equals(currentPuzzle.answer, System.StringComparison.OrdinalIgnoreCase))
            {
                // 答案正确
                Debug.Log($"[解谜] 答案正确! 谜题ID: {currentPuzzle.id}");
                OnPuzzleSolved(currentPuzzle);
            }
            else
            {
                // 答案错误
                attemptsLeft--;
                Debug.LogWarning($"[解谜] 答案错误! 剩余尝试次数: {attemptsLeft}");

                if (attemptsLeft <= 0)
                {
                    Debug.LogError($"[解谜] 谜题失败! 谜题ID: {currentPuzzle.id}");
                    OnPuzzleFailed(currentPuzzle);
                }
                else
                {
                    Debug.Log($"[解谜] 提示: {currentPuzzle.hint}");
                }
            }
        }

        private void OnSkipPuzzle()
        {
            Debug.Log($"[解谜] 跳过谜题! 谜题ID: {currentPuzzleId}");
            OnPuzzleSkipped(puzzles[currentPuzzleId]);
        }

        private void OnPuzzleSolved(Puzzle puzzle)
        {
            isPuzzleActive = false;

            // 显示成功UI
            Debug.Log($"[解谜] 恭喜! 解开谜题: {puzzle.question}");

            // TODO: 发放奖励
            // GameManager.Instance.AddReward(puzzle.reward);

            // 延迟后关闭面板
            if (puzzlePanel != null)
            {
                StartCoroutine(ClosePuzzlePanel());
            }
        }

        private void OnPuzzleFailed(Puzzle puzzle)
        {
            isPuzzleActive = false;

            // 显示失败UI
            Debug.LogError($"[解谜] 谜题失败: {puzzle.question}");

            // TODO: 扣除惩罚
            // GameManager.Instance.DeductPenalty(puzzle.penalty);

            // 延迟后关闭面板
            if (puzzlePanel != null)
            {
                StartCoroutine(ClosePuzzlePanel());
            }
        }

        private void OnPuzzleSkipped(Puzzle puzzle)
        {
            isPuzzleActive = false;

            // 显示跳过UI
            Debug.Log($"[解谜] 跳过谜题: {puzzle.question}");

            // TODO: 扣除跳过惩罚
            // GameManager.Instance.DeductPenalty(puzzle.skipPenalty);

            // 延迟后关闭面板
            if (puzzlePanel != null)
            {
                StartCoroutine(ClosePuzzlePanel());
            }
        }

        private System.Collections.IEnumerator ClosePuzzlePanel()
        {
            yield return new WaitForSeconds(2f);

            if (puzzlePanel != null)
            {
                puzzlePanel.SetActive(false);
            }
        }

        public bool IsPuzzleActive()
        {
            return isPuzzleActive;
        }
    }

    /// <summary>
    /// 谜题数据结构
    /// </summary>
    [System.Serializable]
    public class Puzzle
    {
        public int id;
        public PuzzleType type;
        public string question;
        public string answer;
        public string hint;
        public int difficulty;
        public int reward = 50;
        public int penalty = 10;
        public int skipPenalty = 5;
    }

    /// <summary>
    /// 谜题类型
    /// </summary>
    public enum PuzzleType
    {
        Historical,  // 历史谜题
        TimeTravel,  // 时空谜题
        Logic,       // 逻辑谜题
        Riddle       // 谜语
    }
}
