namespace AdbPacketCapture.Models
{
    public class CaptureOptions
    {
     
        public string Filter { get; set; } = string.Empty;
        public string Interface { get; set; } = "any";

        public string BuildTcpdumpCommand()
        {
            // 캡처 파일 저장 경로 (안드로이드 외부 저장소 사용)
            string captureDir = "/storage/my_capture_Data";
   
            // tcpdump 명령어 구성
            string tcpdumpCommand = $"tcpdump -i {Interface} -w {captureDir}/capture.pcap";
            if (!string.IsNullOrEmpty(Filter))
            {
                if (Filter == "-X")
                {
                    string test = "0";
                }

                tcpdumpCommand += $" {Filter}";
                
            }

            // 최종 명령어 조합 (디렉토리 체크/생성 후 tcpdump 실행)
            return $"{tcpdumpCommand}";
        }
    }
}