## 0.1.2 -> *

* Is it now possible to not trigger the CircuitBreaker with an exception by wrapping it into an CircuitBreakerIgnoreException
* The timeout time can now be set different for each Command by using the ProllyCommand constructor
* The bulkhead pattern is now implemented. Every CommandGroup will have a limited number of requests it is allows to execute in parallel. Default is 10.