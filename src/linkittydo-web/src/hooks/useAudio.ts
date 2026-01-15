import { useRef, useCallback, useEffect } from 'react';

const withBaseUrl = (path: string) => {
  const baseUrl = import.meta.env.BASE_URL || '/';
  const normalizedBaseUrl = baseUrl.endsWith('/') ? baseUrl : `${baseUrl}/`;
  return `${normalizedBaseUrl}${path.replace(/^\//, '')}`;
};

const resolveAudioSrc = (src: string) => {
  if (/^(https?:|data:|blob:)/i.test(src)) return src;
  return withBaseUrl(src);
};

interface UseAudioOptions {
  loop?: boolean;
  volume?: number;
}

export const useAudio = (src: string, options: UseAudioOptions = {}) => {
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const { loop = false, volume = 1.0 } = options;

  useEffect(() => {
    const audio = new Audio(resolveAudioSrc(src));
    audio.loop = loop;
    audio.volume = Math.max(0, Math.min(1, volume));
    audioRef.current = audio;

    return () => {
      audio.pause();
      audio.src = '';
    };
  }, [src, loop, volume]);

  const play = useCallback(() => {
    if (audioRef.current) {
      audioRef.current.currentTime = 0;
      return audioRef.current.play().catch(err => {
        console.warn('Audio playback failed:', err);
      });
    }
  }, []);

  const pause = useCallback(() => {
    if (audioRef.current) {
      audioRef.current.pause();
    }
  }, []);

  const stop = useCallback(() => {
    if (audioRef.current) {
      audioRef.current.pause();
      audioRef.current.currentTime = 0;
    }
  }, []);

  const setVolume = useCallback((newVolume: number) => {
    if (audioRef.current) {
      audioRef.current.volume = Math.max(0, Math.min(1, newVolume));
    }
  }, []);

  return { play, pause, stop, setVolume, audioRef };
};

// Hook for playing a sequence of audio (fanfare then music)
export const useAudioSequence = () => {
  const fanfareRef = useRef<HTMLAudioElement | null>(null);
  const musicRef = useRef<HTMLAudioElement | null>(null);
  const hasStartedRef = useRef(false);
  const initializedRef = useRef(false);

  useEffect(() => {
    // Only initialize once
    if (initializedRef.current) return;
    initializedRef.current = true;

    // Create audio elements in useEffect to ensure we're in browser
    fanfareRef.current = new Audio();
    fanfareRef.current.src = withBaseUrl('audio/fanfare.mp3');
    fanfareRef.current.volume = 0.7;
    fanfareRef.current.preload = 'auto';

    musicRef.current = new Audio();
    musicRef.current.src = withBaseUrl('audio/ice-cream-truck.mp3');
    musicRef.current.loop = true;
    musicRef.current.volume = 0.4;
    musicRef.current.preload = 'auto';

    // Cleanup on unmount
    return () => {
      if (fanfareRef.current) {
        fanfareRef.current.pause();
        fanfareRef.current.src = '';
      }
      if (musicRef.current) {
        musicRef.current.pause();
        musicRef.current.src = '';
      }
    };
  }, []);

  const playSequence = useCallback(() => {
    if (hasStartedRef.current) return;
    hasStartedRef.current = true;

    // Create fresh audio elements on play to avoid stale references
    const fanfare = new Audio(withBaseUrl('audio/fanfare.mp3'));
    fanfare.volume = 0.7;
    
    const music = new Audio(withBaseUrl('audio/ice-cream-truck.mp3'));
    music.loop = true;
    music.volume = 0.4;

    // Store refs for stopping later
    fanfareRef.current = fanfare;
    musicRef.current = music;

    // When fanfare ends, start the looping music
    fanfare.onended = () => {
      music.play().catch(err => {
        console.warn('Music playback failed:', err);
      });
    };

    // Play fanfare first
    fanfare.play().catch(err => {
      console.warn('Fanfare playback failed:', err);
      // Try to play music anyway
      music.play().catch(() => {});
    });
  }, []);

  const stopAll = useCallback(() => {
    hasStartedRef.current = false;
    if (fanfareRef.current) {
      fanfareRef.current.pause();
      fanfareRef.current.currentTime = 0;
    }
    if (musicRef.current) {
      musicRef.current.pause();
      musicRef.current.currentTime = 0;
    }
  }, []);

  const setMusicVolume = useCallback((volume: number) => {
    if (musicRef.current) {
      musicRef.current.volume = Math.max(0, Math.min(1, volume));
    }
  }, []);

  return { playSequence, stopAll, setMusicVolume };
};
