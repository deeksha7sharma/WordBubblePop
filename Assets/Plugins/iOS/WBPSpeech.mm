// Text-to-speech for iOS, called from Speech.cs
#import <AVFoundation/AVFoundation.h>

static AVSpeechSynthesizer *wbpSynth = nil;

extern "C" {

void _wbpSpeak(const char *text, int flush, float rate)
{
    if (text == NULL) return;
    if (wbpSynth == nil) wbpSynth = [[AVSpeechSynthesizer alloc] init];

    if (flush && wbpSynth.isSpeaking)
        [wbpSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];

    NSString *str = [NSString stringWithUTF8String:text];
    AVSpeechUtterance *u = [AVSpeechUtterance speechUtteranceWithString:str];
    u.voice = [AVSpeechSynthesisVoice voiceWithLanguage:@"en-US"];
    float r = AVSpeechUtteranceDefaultSpeechRate * rate;
    if (r < AVSpeechUtteranceMinimumSpeechRate) r = AVSpeechUtteranceMinimumSpeechRate;
    if (r > AVSpeechUtteranceMaximumSpeechRate) r = AVSpeechUtteranceMaximumSpeechRate;
    u.rate = r;
    u.pitchMultiplier = 1.15f;
    [wbpSynth speakUtterance:u];
}

void _wbpStop(void)
{
    if (wbpSynth != nil)
        [wbpSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
}

}
