export function initObserver(component, observerTargetId) {
    var observer = new IntersectionObserver(e => {
        component.invokeMethodAsync('OnIntersection');
    });
    let element = document.getElementById(observerTargetId);
    if (element == null) throw new Error("The observable target was not found");
    observer.observe(element);
    window.Observer = observer;
    return observer;

}

