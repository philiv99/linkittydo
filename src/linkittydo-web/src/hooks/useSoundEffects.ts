import { useCallback, useRef, useEffect } from 'react';

const MUTE_KEY = 'linkittydo_sound_muted';

const getStoredMuteState = (): boolean => {
  try {
    return localStorage.getItem(MUTE_KEY) === 'true';
  } catch {
    return false;
  }
};

/**
 * Hook for generating game sound effects using the Web Audio API.
 * Produces lightweight tones for correct/incorrect guesses and game events.
 * Respects prefers-reduced-motion and mute preference.
 */
export const useSoundEffects = () => {
  const ctxRef = useRef<AudioContext | null>(null);
  const mutedRef = useRef(getStoredMuteState());

  const getContext = useCallback(() => {
    if (!ctxRef.current || ctxRef.current.state === 'closed') {
      ctxRef.current = new AudioContext();
    }
    if (ctxRef.current.state === 'suspended') {
      ctxRef.current.resume();
    }
    return ctxRef.current;
  }, []);

  const shouldPlay = useCallback(() => {
    if (mutedRef.current) return false;
    if (typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) {
      return false;
    }
    return true;
  }, []);

  const playTone = useCallback((frequency: number, duration: number, type: OscillatorType = 'sine', volume = 0.3) => {
    if (!shouldPlay()) return;
    try {
      const ctx = getContext();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = type;
      osc.frequency.value = frequency;
      gain.gain.value = volume;
      gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + duration);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start();
      osc.stop(ctx.currentTime + duration);
    } catch {
      // Silently fail if audio context is not available
    }
  }, [getContext, shouldPlay]);

  const playCorrect = useCallback(() => {
    // Rising two-tone: pleasant "ding"
    playTone(523, 0.15, 'sine', 0.25); // C5
    setTimeout(() => playTone(659, 0.2, 'sine', 0.25), 100); // E5
  }, [playTone]);

  const playIncorrect = useCallback(() => {
    // Low buzz
    playTone(200, 0.25, 'square', 0.15);
  }, [playTone]);

  const playSolved = useCallback(() => {
    // Ascending arpeggio
    playTone(523, 0.15, 'sine', 0.3); // C5
    setTimeout(() => playTone(659, 0.15, 'sine', 0.3), 120); // E5
    setTimeout(() => playTone(784, 0.15, 'sine', 0.3), 240); // G5
    setTimeout(() => playTone(1047, 0.3, 'sine', 0.3), 360); // C6
  }, [playTone]);

  const playGaveUp = useCallback(() => {
    // Descending tone
    playTone(440, 0.2, 'sine', 0.2); // A4
    setTimeout(() => playTone(349, 0.2, 'sine', 0.2), 150); // F4
    setTimeout(() => playTone(262, 0.3, 'sine', 0.2), 300); // C4
  }, [playTone]);

  const setMuted = useCallback((muted: boolean) => {
    mutedRef.current = muted;
    try {
      localStorage.setItem(MUTE_KEY, String(muted));
    } catch {
      // Ignore localStorage errors
    }
  }, []);

  const isMuted = useCallback(() => mutedRef.current, []);

  // Cleanup
  useEffect(() => {
    return () => {
      if (ctxRef.current && ctxRef.current.state !== 'closed') {
        ctxRef.current.close();
      }
    };
  }, []);

  return { playCorrect, playIncorrect, playSolved, playGaveUp, setMuted, isMuted };
};
