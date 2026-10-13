import {
  GoogleLogin,
  type CredentialResponse,
} from "@react-oauth/google";
import { useEffect, useRef, useState } from "react";

/**
 * Nút Google GSI có chiều rộng theo pixel — đo container để thu nhỏ cho
 * vừa màn hình hẹp (300px cứng sẽ tràn khỏi thẻ ở điện thoại 320–375px).
 */
export function GoogleLoginButton({
  onSuccess,
  onError,
}: {
  onSuccess: (r: CredentialResponse) => void;
  onError: () => void;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const [width, setWidth] = useState(300);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const update = () =>
      setWidth(Math.max(240, Math.min(300, el.clientWidth)));
    update();
    const ro = new ResizeObserver(update);
    ro.observe(el);
    return () => ro.disconnect();
  }, []);

  return (
    <div ref={ref} className="flex w-full justify-center">
      <GoogleLogin onSuccess={onSuccess} onError={onError} width={width} />
    </div>
  );
}
